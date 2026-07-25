using HarmonyLib;
using HUD;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow;

public class RMOverlayHUDMenu : Menu.Menu, IOwnAHUD
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
    
    public static bool TryGetOverlayMenu(out RMOverlayHUDMenu overlayHUDOwner) => (overlayHUDOwner = overlayMenu) is not null;
    public static RMOverlayHUDMenu GetOverlayMenu() => overlayMenu;
    public static bool TryGetOverlay(out RMOverlayHUD overlayHUD) => (overlayHUD = overlayMenu?.overlayHUD) is not null;
    public static RMOverlayHUD GetOverlay() => overlayMenu?.overlayHUD;
    
    private static RMOverlayHUDMenu overlayMenu;
    public RMOverlayHUDMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.RainMeadowOverlay)
    {
        this.rainWorld = manager.rainWorld;
        overlayMenu?.Destroy();
        overlayMenu = this;
        this.pages.Add(new Menu.Page(this, null, "Overlay", 0));
    }

    // Functions
    public void AddOverlayHUD()
    {
        this.overlayHUD?.ClearAllSprites();
        this.overlayHUD = new(rainWorld, this); 
    }
    public void RemoveOverlayHUD()
    {
        this.overlayHUD?.ClearAllSprites();
        this.overlayHUD = null;
    }

    public override void Update()
    {
        base.Update();
        this.overlayHUD?.Update();
        if (this.container is not null)
        {
            if (Futile.stage.GetChildAt(Futile.stage.GetChildCount() - 1) != this.cursorContainer)
            {
                // Keep them on top
                Futile.stage.AddChild(this.container);
                Futile.stage.AddChild(this.cursorContainer);
            }
        }
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        this.overlayHUD?.Draw(timeStacker);
    }
    public override bool FreezeMenuFunctions => true; // no input update, they can handle it
    public void Destroy()
    {
        this.RemoveOverlayHUD();
        this.ShutDownProcess();
        overlayMenu = null;
    }
    
    public readonly RainWorld rainWorld;
    public RMOverlayHUD? overlayHUD;
}

public class RMOverlayHUD : HUD.HUD
{
    public static bool TryGetOverlay(out RMOverlayHUD overlayHUD) => RMOverlayHUDMenu.TryGetOverlay(out overlayHUD);
    public static RMOverlayHUD GetOverlay() => RMOverlayHUDMenu.GetOverlay();
    public RMOverlayHUD(RainWorld rainWorld, RMOverlayHUDMenu owner) : base([owner.container], rainWorld, owner)
    {
        
    }

    public void AddChatHUD(RoomCamera roomCamera)
    {
        DestroyChatHUD();
        this.chatHud = new ChatHud(roomCamera);
        this.AddPart(chatHud);
    }
    public void SetNewChatHUDCamera(RoomCamera roomCamera)
    {
        if (chatHud is not null)
        {
            RainMeadow.Debug("Gave new camera to chat HUD");
            chatHud.UpdateCamera(roomCamera);
        }
    }
    public void DestroyChatHUD()
    {
        chatHud?.ClearSprites();
        this.parts.Remove(this.chatHud);
        this.chatHud = null;
    }
    public ChatHud? chatHud;
    public bool isFocusedOnMenu => chatHud?.chatInputActive ?? false;
}
