using System;
using System.Collections.Generic;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
	public static class DebugOverlay
	{
		private static FContainer overlayContainer;

		private class ResourceNode
		{
			public OnlineResource resource;
			private FSprite lineSprite;
			private FSprite lineSprite2;
			private FLabel label;
			public string text = "";
			public Vector2 pos;
			public Color color = Color.white;
			public float thickness = 3;
			public int lines = 0;
			public int entityCount = 0;

			public float width => label.textRect.width;
			public ResourceNode(RainWorld rainWorld, FContainer container, OnlineResource resource)
			{
				this.resource = resource;

				lineSprite = new FSprite("pixel");
				lineSprite2 = new FSprite("pixel");

				label = new FLabel(Custom.GetFont(), resource.ToString());
				label.alignment = FLabelAlignment.Left;
				label.color = Color.white;

				container.AddChild(lineSprite);
				container.AddChild(lineSprite2);
				container.AddChild(label);
			}

			public void Update()
			{
				label.x = pos.x + 0.01f + 20;
				label.y = pos.y;

				label.color =
					resource.isOwner ? Color.green :
					resource.isSupervisor ? Color.blue :
					resource.isFree ? Color.yellow :
					resource.canRelease ? Color.red : Color.white;

				lineSprite.x = pos.x + 20;
				lineSprite.y = pos.y - 8;
				lineSprite.scaleX = 2;
				lineSprite.anchorY = 1;
				lineSprite.scaleY = Mathf.Max(lines * 35 - 8, 0);

				lineSprite2.x = pos.x;
				lineSprite2.y = pos.y;
				lineSprite2.anchorX = 0;
				lineSprite2.scaleX = 15;
				lineSprite2.scaleY = 2;
				lineSprite2.color = OnlineManager.feeds.Exists(sub => sub.resource == resource) ? Color.green : Color.white;

				lines = 0;
				entityCount = 0;
			}

			public void RemoveSprites()
			{
				lineSprite.RemoveFromContainer();
				lineSprite2.RemoveFromContainer();
				label.RemoveFromContainer();
			}
		}

		private static List<ResourceNode> resourceNodes = new List<ResourceNode>();

		private class EntityNode
		{
			public OnlineEntity entity;
			private IconSymbol iconSymbol;
			private FLabel label;
			public string text = "";
			public Vector2 pos;
			public Color color = Color.white;
			public float rad = 5;
			public float thickness = 3;
			public EntityNode(RainWorld rainWorld, FContainer container, OnlineEntity onlineEntity)
			{
				this.entity = onlineEntity;

				if (onlineEntity is OnlinePhysicalObject onlinePhysicalObject)
				{
					if (onlinePhysicalObject.apo is AbstractCreature creature)
					{
						iconSymbol = new CreatureSymbol(CreatureSymbol.SymbolDataFromCreature(creature), container);
					}
					else
					{
						iconSymbol = new ItemSymbol(ItemSymbol.SymbolDataFromItem(onlinePhysicalObject.apo).GetValueOrDefault(), container);
					}
					iconSymbol.Show(true);
					iconSymbol.showFlash = iconSymbol.lastShowFlash = 0;
				}

				label = new FLabel(Custom.GetFont(), text);
				label.color = Color.white;

				container.AddChild(label);
			}

			public void Update()
			{
				float alpha = entity.isMine ? 1 : 0.5f;
				iconSymbol.symbolSprite.alpha = alpha;
				iconSymbol.shadowSprite1.alpha = alpha;
				iconSymbol.shadowSprite2.alpha = alpha;
				iconSymbol.Draw(1, pos + new Vector2(0.5f, 0));

				label.x = pos.x + 0.01f;
				label.y = pos.y + 20;
				label.text = text;
			}

			public void RemoveSprites()
			{
				iconSymbol.RemoveSprites();
				label.RemoveFromContainer();
			}
		}

		private static List<EntityNode> entityNodes = new List<EntityNode>();
		private static List<FLabel> outgoingLabels = new List<FLabel>();
		private static List<FLabel> incomingLabels = new List<FLabel>();

		public class playerCache
		{
			public List<Individual> items;
			public static TimeSpan itemsExpiralDate;
			public class Individual
			{
				public OnlinePlayer player;
				public DateTime expiralDate;

				public bool timeToDelete => DateTime.Now > expiralDate;
				public Individual(OnlinePlayer Player)
				{
					player = Player;
					expiralDate = DateTime.Now + itemsExpiralDate;
				}
			}

			public playerCache(int size, TimeSpan expiry)
			{
				items = new List<Individual>(size);
				itemsExpiralDate = expiry;
			}

			public void addPlayer(OnlinePlayer Player)
			{
				if (items.Exists(x => x.player == Player))
				{
					items.Find(x => x.player == Player).expiralDate = DateTime.Now + itemsExpiralDate;
					return;
				}
				items.Add(new Individual(Player));
			}

			public void removeExpired()
			{
				items.RemoveAll(x => x.timeToDelete);
			}
		}

		public static playerCache playersRead = new playerCache(16, TimeSpan.FromSeconds(5));
		public static playerCache playersWritten = new playerCache(16, TimeSpan.FromSeconds(5));

		public static void Update(RainWorldGame self, float dt)
		{
			if (overlayContainer == null && self.devToolsActive)
			{
				CreateOverlay(self);
			}

			if (overlayContainer != null && !self.devToolsActive)
			{
				RemoveOverlay(self);
			}

			if (overlayContainer == null)
				return;

			Vector2 screenSize = self.rainWorld.options.ScreenSize;

			outgoingLabels.ForEach(label => label.RemoveFromContainer());
			outgoingLabels.Clear();
			incomingLabels.ForEach(label => label.RemoveFromContainer());
			incomingLabels.Clear();

			playersWritten.removeExpired();
			playersRead.removeExpired();

			int line = 0;
			foreach (playerCache.Individual idv in playersWritten.items)
			{
				var player = idv.player;
				var playerTruePing = Math.Max(1, player.ping - 16);
                var averageBytes = 0;
                foreach (var bytes in player.bytesOut) {
                    averageBytes += bytes;
                }
                // averageBytes = bytes per 40 frames
                averageBytes = (int)((float)averageBytes / 40 * OnlineManager.instance.framesPerSecond); // bytes per second
                var averageBits = averageBytes * 8;

				FLabel label = new FLabel(Custom.GetFont(), $"{player} ({averageBits / 1000}kbps - {playerTruePing}ms)")
				{
					x = 5.01f,
					y = screenSize.y - 25 - 15 * line,
					alignment = FLabelAlignment.Left,
                    color = 
                        player.eventsWritten ? new Color(1, 0.5f, 0) :
                        player.statesWritten ? Color.white : 
                        Color.grey
				};

				overlayContainer.AddChild(label);
				outgoingLabels.Add(label);
				line++;
			}

			line = 0;
			foreach (playerCache.Individual idv in playersRead.items)
			{
				var player = idv.player;
				var playerTruePing = Math.Max(1, player.ping - 16);
                var averageBytes = 0;
                foreach (var bytes in player.bytesIn) {
                    averageBytes += bytes;
                }
                // averageBytes = bytes per 40 frames
                averageBytes = (int)((float)averageBytes / 40 * OnlineManager.instance.framesPerSecond); // bytes per second
                var averageBits = averageBytes * 8;

				FLabel label = new FLabel(Custom.GetFont(), $"{player} ({averageBits / 1000}kbps - {playerTruePing}ms)")
				{
					x = 205.01f,
					y = screenSize.y - 25 - 15 * line,
					alignment = FLabelAlignment.Left,
                    color = 
                        player.eventsRead ? new Color(1, 0.5f, 0) :
                        player.statesRead ? Color.white : 
                        Color.grey
				};

				overlayContainer.AddChild(label);
				incomingLabels.Add(label);
				line++;
			}

			resourceNodes.RemoveAll(node =>
			{
				if (!node.resource.isActive)
				{
					node.RemoveSprites();
					return true;
				}
				return false;
			});

			var root = resourceNodes[0];

			// Worlds (Regions)

			RoomSession inRoomSession;
			WorldSession inWorldSession = null;
			if (RoomSession.map.TryGetValue(self.cameras[0].room.abstractRoom, out inRoomSession))
			{
				inWorldSession = inRoomSession.worldSession;
			}

			var worlds = OnlineManager.lobby.worldSessions.Values.ToList();
			worlds.Sort((x, y) => (x == inWorldSession ? -1 : 0) + (y == inWorldSession ? 1 : 0));

			int lastWorldLines = 0;
			foreach (var worldSession in worlds)
			{
				if (!worldSession.isActive)
					continue;

				ResourceNode regionNode = resourceNodes.Find(regionNode => regionNode.resource == worldSession);
				if (regionNode == null)
				{
					regionNode = new ResourceNode(self.rainWorld, overlayContainer, worldSession);
					resourceNodes.Add(regionNode);
				}

				root.lines += lastWorldLines + 1;
				regionNode.pos = root.pos + new Vector2(20, root.lines * -35);

				// Rooms
				var rooms = worldSession.roomSessions.Values.ToList();
				rooms.Sort((x, y) => (x == inRoomSession ? -1 : 0) + (y == inRoomSession ? 1 : 0));
				foreach (var roomSession in rooms)
				{
					if (!roomSession.isActive)
						continue;

					ResourceNode roomNode = resourceNodes.Find(node => node.resource == roomSession);
					if (roomNode == null)
					{
						roomNode = new ResourceNode(self.rainWorld, overlayContainer, roomSession);
						resourceNodes.Add(roomNode);
					}

					regionNode.lines++;
					roomNode.pos = regionNode.pos + new Vector2(20, regionNode.lines * -35);
				}

				lastWorldLines = regionNode.lines;
			}

			foreach (var node in resourceNodes)
			{
				node.Update();
			}

			var onlineEntities = OnlineManager.recentEntities.Values.ToList();
			onlineEntities.Sort((x, y) =>
			{
				int comp = (x is OnlineCreature ? -1 : 0) + (y is OnlineCreature ? 1 : 0);
				if (comp == 0)
				{
					comp = (int)((OnlinePhysicalObject)x).apo.type - (int)((OnlinePhysicalObject)y).apo.type;
					if (comp == 0)
                    {
                        if (x is OnlineCreature && y is OnlineCreature) {
                            comp = (int)((AbstractCreature)((OnlineCreature)x).apo).creatureTemplate.type - (int)((AbstractCreature)((OnlineCreature)y).apo).creatureTemplate.type;
                            if (comp == 0) {
                                comp = (x.isMine ? -1 : 0) + (y.isMine ? 1 : 0);
                            }
                        }
                        else
                        {
                            comp = (x.isMine ? -1 : 0) + (y.isMine ? 1 : 0);
                        }
                    }
				}

				return comp;
			});
			foreach (var onlineEntity in onlineEntities)
			{
				ResourceNode resourceNode = resourceNodes.Find(node => node.resource == onlineEntity.currentlyJoinedResource);
				if (resourceNode != null && onlineEntity is OnlinePhysicalObject onlinePhysicalObject)
				{

					EntityNode entityNode = entityNodes.Find(node => node.entity == onlineEntity);
					if (entityNode == null)
					{

						bool isMe = false;
						if (onlineEntity.isMine)
						{
							if (onlinePhysicalObject.apo.type == AbstractPhysicalObject.AbstractObjectType.Creature)
							{
								AbstractCreature creature = (AbstractCreature)onlinePhysicalObject.apo;

								if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat)
								{
									isMe = true;
								}
							}
						}

						entityNode = new EntityNode(self.rainWorld, overlayContainer, onlineEntity)
						{
							text = isMe ? "YOU" : ""
						};
						entityNodes.Add(entityNode);
					}

					entityNode.pos = resourceNode.pos + new Vector2(40 + 20 * resourceNode.entityCount + resourceNode.width, 0);
					resourceNode.entityCount++;
				}
			}

			entityNodes.RemoveAll(node =>
			{
				if (!OnlineManager.recentEntities.ContainsValue(node.entity) || node.entity.primaryResource == null)
				{
					node.RemoveSprites();
					return true;
				}
				return false;
			});

			foreach (var node in entityNodes)
			{
				node.Update();
			}
		}

		public static void CreateOverlay(RainWorldGame self)
		{
			Vector2 screenSize = self.rainWorld.options.ScreenSize;
			overlayContainer = new FContainer();

			overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Outgoing (Receivers)")
			{
				alignment = FLabelAlignment.Left,
				x = 5.01f,
				y = screenSize.y - 10,
			});
			overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Incoming (Senders)")
			{
				alignment = FLabelAlignment.Left,
				x = 205.01f,
				y = screenSize.y - 10,
			});

			// Lobby (Root)
			resourceNodes.Add(new ResourceNode(self.rainWorld, overlayContainer, OnlineManager.lobby)
			{
				pos = new Vector2(400, screenSize.y - 30),
			});

			Futile.stage.AddChild(overlayContainer);
		}

		public static void RemoveOverlay(RainWorldGame self)
		{
			resourceNodes.Clear();
			entityNodes.Clear();
			overlayContainer?.RemoveFromContainer();
			overlayContainer = null;
		}
	}
}
