using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RainMeadow {
	static class DebugOverlay {
		static FContainer overlayContainer;


		class ResourceNode {
			public OnlineResource resource;
			FSprite sprite;
			FLabel label;
			public string text = "";
			public Vector2 pos;
			public Color color = Color.white;
			public float rad = 10;
			public float thickness = 3;
			public int entityCount = 0;
			public ResourceNode (RainWorld rainWorld, FContainer container) {
				sprite = new FSprite("Futile_White", true);
				sprite.shader = rainWorld.Shaders["VectorCircleFadable"];
				sprite.color = new Color(0,0, 1);

				label = new FLabel(Custom.GetFont(), text);
				label.color = Color.white;

				container.AddChild(sprite);
				container.AddChild(label);
			}

			public void Update() {
				sprite.x = pos.x;
				sprite.y = pos.y;
				label.x = pos.x + 0.01f;
				label.y = pos.y;

				this.sprite.scale = rad / 8f;
				if (thickness == -1f) {
					this.sprite.alpha = 1f;
				} else if (rad > 0f) {
					this.sprite.alpha = thickness / rad;
				} else {
					this.sprite.alpha = 0f;
				}

				label.text = text;
				sprite.color = new Color((
					resource.isOwner ? 2f :
					resource.isSupervisor ? 3f :
					resource.isFree ? 4f :
					resource.canRelease ? 1f : 0) / 255,0, 1);
				label.color = OnlineManager.feeds.Exists(sub => sub.resource == resource) ? Color.green : Color.white;
			}

			public void RemoveSprites() {
				sprite.RemoveFromContainer();
				label.RemoveFromContainer();
			}
		}

		static List<ResourceNode> resourceNodes = new List<ResourceNode>();

		class EntityNode {
			public OnlineEntity entity;
			FSprite sprite;
			FLabel label;
			public string text = "";
			public Vector2 pos;
			public Color color = Color.white;
			public float rad = 5;
			public float thickness = 3;
			public EntityNode (RainWorld rainWorld, FContainer container) {
				sprite = new FSprite("Futile_White", true);
				sprite.shader = rainWorld.Shaders["VectorCircleFadable"];
				sprite.color = new Color(0,0, 1);

				label = new FLabel(Custom.GetFont(), text);
				label.color = Color.white;

				container.AddChild(sprite);
				container.AddChild(label);
			}

			public void Update() {
				sprite.x = pos.x;
				sprite.y = pos.y;
				label.x = pos.x + 0.01f;
				label.y = pos.y + rad + thickness + 2;

				this.sprite.scale = rad / 8f;
				if (thickness == -1f) {
					this.sprite.alpha = 1f;
				} else if (rad > 0f) {
					this.sprite.alpha = thickness / rad;
				} else {
					this.sprite.alpha = 0f;
				}

				label.text = text;
				sprite.color = new Color((entity.owner.isMe ? 2f : 0) / 255,0, 1);
			}
			
			public void RemoveSprites() {
				sprite.RemoveFromContainer();
				label.RemoveFromContainer();
			}
		}

		static List<EntityNode> entityNodes = new List<EntityNode>();

		static List<FLabel> outgoingLabels = new List<FLabel>();
		static List<FLabel> incomingLabels = new List<FLabel>();

		public static void Update(RainWorldGame self, float dt) {
			if (overlayContainer == null && self.devToolsActive) {
				CreateOverlay(self);
			}

			if (overlayContainer != null && !self.devToolsActive) {
				RemoveOverlay(self);
			}

			if (overlayContainer != null) {
				Vector2 screenSize = self.rainWorld.options.ScreenSize;
				
				outgoingLabels.ForEach(label => label.RemoveFromContainer());
				outgoingLabels.Clear();
				incomingLabels.ForEach(label => label.RemoveFromContainer());
				incomingLabels.Clear();

                if (UnityEngine.Input.GetKey(KeyCode.LeftAlt))
                {
                    FLabel label = new FLabel(Custom.GetFont(), "!! CONNECTION CUT !!") { x = screenSize.x / 2, y = 15.0f};
                    overlayContainer.AddChild(label);
                    outgoingLabels.Add(label);
                }

                int line = 0;
				foreach (OnlinePlayer player in PlayersManager.players) {
					if (player.statesWritten || player.eventsWritten) {
						FLabel label = new FLabel(Custom.GetFont(), player.ToString()) { alignment = FLabelAlignment.Left, x = 5.01f, y = screenSize.y - 25 - 15 * line };
						if (player.eventsWritten) {
							label.color = new Color(1, 0.5f, 0);
						}
						overlayContainer.AddChild(label);
						outgoingLabels.Add(label);
						line++;
					}
				}

				line = 0;
				foreach (OnlinePlayer player in PlayersManager.players) {
					if (player.statesRead || player.eventsRead) {
						FLabel label = new FLabel(Custom.GetFont(), player.ToString()) { alignment = FLabelAlignment.Left, x = 155.01f, y = screenSize.y - 25 - 15 * line };
						if (player.eventsRead) {
							label.color = new Color(1, 0.5f, 0);
						}
						overlayContainer.AddChild(label);
						incomingLabels.Add(label);
						line++;
					}
				}

				// Worlds (Regions)
				int worldShift = -30;
				foreach (var worldSession in OnlineManager.lobby.worldSessions) {
					if (!worldSession.Value.isActive)
						continue;

					worldShift += 30;

					ResourceNode regionNode = resourceNodes.Find(regionNode => regionNode.resource == worldSession.Value);
					if (regionNode == null) {
						regionNode = new ResourceNode(self.rainWorld, overlayContainer) {
							resource = worldSession.Value,
							rad = 15,
							text = worldSession.Key,
						};
						resourceNodes.Add(regionNode);
					}
					
					regionNode.pos = new Vector2(300 + worldShift, screenSize.y - 90);

					// Rooms
					int roomShift = -70;
					foreach (var roomSession in worldSession.Value.roomSessions) {
						if (!roomSession.Value.isActive)
							continue;

						roomShift += 70;

						ResourceNode roomNode = resourceNodes.Find(node => node.resource == roomSession.Value);
						if (roomNode == null) {
							roomNode = new ResourceNode(self.rainWorld, overlayContainer) {
								resource = roomSession.Value,
								rad = 30,
								text = roomSession.Key,
							};
							resourceNodes.Add(roomNode);
						}

						roomNode.pos = new Vector2(300 + worldShift + roomShift, screenSize.y - 150);
						if (roomSession.Key == self.cameras[0].room.abstractRoom.name) {
							roomNode.thickness = 4;
						} else {
							roomNode.thickness = 2;
						}
					}

					worldShift += roomShift;
				}

				resourceNodes.RemoveAll(node => { 
					if (!node.resource.isActive) {
						node.RemoveSprites();
						return true;
					}
					return false;
				});

				foreach (var node in resourceNodes) {
					node.Update();
					node.entityCount = 0;
				}

				foreach (var mappedEntity in OnlineManager.recentEntities) {
					OnlineEntity onlineEntity = mappedEntity.Value;
					ResourceNode resourceNode = resourceNodes.Find(node => node.resource == onlineEntity.currentlyJoinedResource);
					if (resourceNode != null && onlineEntity is OnlinePhysicalObject onlinePhysicalObject) {
						
						EntityNode entityNode = entityNodes.Find(node => node.entity == onlineEntity);
						if (entityNode == null) {
							
							bool isMe = false;
							if (onlineEntity.owner.isMe) {
								if (onlinePhysicalObject.apo.type == AbstractPhysicalObject.AbstractObjectType.Creature) {
									AbstractCreature creature = (AbstractCreature)onlinePhysicalObject.apo;

									if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) {
										isMe = true;
									}
								}
							}

							entityNode = new EntityNode(self.rainWorld, overlayContainer) {
								entity = onlineEntity,
								text = isMe ? "YOU" : onlinePhysicalObject.apo.type == AbstractPhysicalObject.AbstractObjectType.Creature ? ((AbstractCreature)onlinePhysicalObject.apo).creatureTemplate.type.ToString() : onlinePhysicalObject.apo.type.ToString(),
							};
							entityNodes.Add(entityNode);
						}

						entityNode.pos = resourceNode.pos + new Vector2(0, - 50 - 20 * resourceNode.entityCount);

						resourceNode.entityCount++;
					}
				}

				entityNodes.RemoveAll(node => {
					if (!OnlineManager.recentEntities.ContainsValue(node.entity) || node.entity.primaryResource == null) {
						node.RemoveSprites();
						return true;
					}
					return false;
				});

				foreach (var node in entityNodes) {
					node.Update();
				}
			}
		}

		public static void CreateOverlay(RainWorldGame self) {
			Vector2 screenSize = self.rainWorld.options.ScreenSize;
			overlayContainer = new FContainer();

			overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Outgoing (Receivers)") {
				alignment = FLabelAlignment.Left,
				x = 5.01f,
				y = screenSize.y - 10,
			});
			overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Incoming (Senders)") {
				alignment = FLabelAlignment.Left,
				x = 155.01f,
				y = screenSize.y - 10,
			});

			// Lobby (Root)
			resourceNodes.Add(new ResourceNode(self.rainWorld, overlayContainer) {
				resource = OnlineManager.lobby,
				pos = new Vector2(x: 300, screenSize.y - 30),
				rad = 20,
				text = ".",
			});

			Futile.stage.AddChild(overlayContainer);
		}

		public static void RemoveOverlay(RainWorldGame self) {
			resourceNodes.Clear();
			entityNodes.Clear();
			overlayContainer.RemoveFromContainer();
			overlayContainer = null;
		}
	}
}
