using HarmonyLib;
using HUD;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow;

public class RMOverlayHUDOwner : IOwnAHUD
{
    // Interface
    public int CurrentFood => 0;

    public Player.InputPackage MapInput => new();

    public bool RevealMap => false;

    public Vector2 MapOwnerInRoomPosition => new();

    public bool MapDiscoveryActive => false;

    public int MapOwnerRoom => 0;

    public void FoodCountDownDone() {}

    public HUD.HUD.OwnerType GetOwnerType() => RainMeadow.Ext_HUD_OwnerType.RainMeadowOverlay;

    public void PlayHUDSound(SoundID soundID)
    {
        if (this.rainWorld.processManager.menuMic != null)
        {
            this.rainWorld.processManager.menuMic.PlaySound(soundID);
        }
        else if (this.rainWorld.processManager.currentMainLoop is RainWorldGame game)
        {
            game.cameras[0].virtualMicrophone.PlaySound(soundID, 0f, 1f, 1f, 1);
        }
    }

    // ctor
    public static ConditionalWeakTable<RainWorld, RMOverlayHUDOwner> overlayManagers = new();
    public static bool TryGetOverlayOwner(RainWorld rainWorld, out RMOverlayHUDOwner overlayHUDOwner) 
        => overlayManagers.TryGetValue(rainWorld, out overlayHUDOwner);
    public static RMOverlayHUDOwner? GetOverlayOwner(RainWorld rainWorld) 
        { TryGetOverlayOwner(rainWorld, out var overlayHUDOwner); return overlayHUDOwner;}
    public static bool TryGetOverlay(RainWorld rainWorld, out RMOverlayHUD overlayHUD) 
        { overlayHUD = GetOverlayOwner(rainWorld)?.overlayHUD; return overlayHUD is not null;}
    public static RMOverlayHUD? GetOverlay(RainWorld rainWorld) 
        { TryGetOverlay(rainWorld, out var overlayHUD); return overlayHUD;}
    public RMOverlayHUDOwner(RainWorld rainWorld)
    {
        this.rainWorld = rainWorld;
        if (overlayManagers.TryGetValue(rainWorld, out var oldOverlayManager)) oldOverlayManager.Destroy();
        overlayManagers.Add(rainWorld, this);
    }

    // Functions
    public void ClearFContainers()
    {
        if (this.spriteContainers is not null)
        {
            for (int i = 0; i < this.spriteContainers.Length; i++)
            {
                Futile.stage.RemoveChild(this.spriteContainers[i]);
            }
        }
        this.spriteContainers = null;
    }
    public void AddOverlayHUD()
    {
        ClearFContainers();

        this.spriteContainers = new FContainer[overlayLayers];
        for (int i = 0; i < this.spriteContainers.Length; i++)
        {
            this.spriteContainers[i] = new FContainer();
            Futile.stage.AddChild(this.spriteContainers[i]);
        }

        this.overlayHUD?.ClearAllSprites();
        this.overlayHUD = new(rainWorld, this); 
    }
    public void RemoveOverlayHUD()
    {
        ClearFContainers();
        this.overlayHUD?.ClearAllSprites();
        this.overlayHUD = null;
    }

    public void Update(float dt) // from MainProcessLoop
    {
        this.myTimeStacker += dt * FPS;
		int overload = 0;
		while (this.myTimeStacker > 1f)
		{
			this.overlayHUD?.Update();
			this.myTimeStacker -= 1f;

			overload++;
			if (overload > overloadLimit) this.myTimeStacker = 0f;
		}
		this.GrafUpdate(this.myTimeStacker);

        if (this.spriteContainers is not null)
        {
            if (Futile.stage.GetChildAt(Futile.stage.GetChildCount() - 1) != this.spriteContainers[overlayLayers - 1])
            {
                // Keep them on top
                for (int i = 0; i < this.spriteContainers.Length; i++)
                {
                    Futile.stage.AddChild(this.spriteContainers[i]);
                }
            }
        }
    }
    public void GrafUpdate(float timeStacker)
    {
        this.overlayHUD?.Draw(timeStacker);
    }
    public void Destroy()
    {
        RemoveOverlayHUD();
        if (overlayManagers.TryGetValue(rainWorld, out _))
        {
            overlayManagers.Remove(rainWorld);
        }
    }
    
    public readonly RainWorld rainWorld;
    public FContainer[]? spriteContainers;
    public RMOverlayHUD? overlayHUD;
    public float myTimeStacker;

    public const int overlayLayers = 3;
    public const int FPS = 40;
    public const int overloadLimit = 3;
}

public class RMOverlayHUD : HUD.HUD
{
    public RMOverlayHUD(RainWorld rainWorld, RMOverlayHUDOwner owner) : base(owner.spriteContainers, rainWorld, owner)
    {
        
    }

    public void AddChatHUD(RoomCamera roomCamera)
    {
        DestroyChatHUD();
        this.chatHud = new ChatHud(this, roomCamera);
        this.AddPart(chatHud);
    }
    public void SetNewChatHUDCamera(RoomCamera roomCamera)
    {
        if (chatHud is not null)
        {
            RainMeadow.Debug("Gave new camera to chat HUD");
            chatHud.camera = roomCamera;
        }
    }
    public void DestroyChatHUD()
    {
        chatHud?.ClearSprites();
    }
    public ChatHud? chatHud;
    public bool isFocusedOnMenu => chatHud?.chatInputActive ?? false;
}
