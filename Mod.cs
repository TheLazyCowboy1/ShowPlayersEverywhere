using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShowPlayersEverywhere;

//dependencies:
//Rain Meadow:
[BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]


[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public class Mod : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.ShowPlayersEverywhere";
    public const string MOD_NAME = "Show Players Everywhere";
    public const string MOD_VERSION = "0.0.2";

    #region setup
    public Mod()
    {
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void OnDisable()
    {
        //Remove hooks

        On.RainWorld.OnModsInit -= RainWorldOnOnModsInit;

        if (IsInit)
        {
            On.HUD.Map.Draw -= Map_Draw;
        }
    }

    private bool IsInit;
    public static bool MeadowEnabled = false;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return; //prevents adding hooks twice

            On.HUD.Map.Draw += Map_Draw;

            MeadowEnabled = ModManager.ActiveMods.Exists(mod => mod.id == "henpemaz_rainmeadow");

            IsInit = true;

            Logger.LogDebug("Hooks added!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    #endregion
    #region hooks
    //intended to generalize players found through Rain Meadow and players found normally
    public struct PlayerAvatarData
    {
        public AbstractCreature player;
        public Color color;
        public PlayerAvatarData(AbstractCreature _player, Color _color)
        {
            player = _player;
            color = _color;
        }
    }
    private void Map_Draw(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
    {
        orig(self, timeStacker);

        if (self.fade <= 0)
            return; //if the map isn't being drawn, don't draw its symbols!
        if (self.hud.owner is not Player owner)
            return;
        if (owner?.room?.game is null)
            return;

        //step 1: make a list of PlayerAvatarData, so we can generalize the code
        List<PlayerAvatarData> avatarList = new();
        foreach (var player in owner.room.game.Players)
        {
            if (player == owner.abstractCreature) continue; //don't draw myself
            if (player.realizedCreature is Player p)
                avatarList.Add(new(player, RainWorld.PlayerObjectBodyColors[p.playerState.playerNumber]));
            else
                avatarList.Add(new(player, Color.white));
        }

        //step 2: add meadow player avatars
        try
        {
            if (MeadowEnabled && MeadowCompat.IsOnline)
            {
                var newAvatars = MeadowCompat.GetAvatarData();
                foreach (var avatar in newAvatars)
                {
                    //had to use a pos check, because other checks weren't working for some reason
                    if (avatar.player.world.name == owner.abstractCreature.world.name //don't want to draw players in other regions
                        //&& avatar.player.pos != owner.abstractCreature.pos
                        && !avatarList.Exists(a => a.player.pos == avatar.player.pos))
                    {
                        avatarList.Add(avatar);
                    }
                }
                newAvatars.Clear();
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }

        //step 3: remove all player sprites already added
        foreach (var symbol in self.creatureSymbols)
        {
            if (symbol.iconData.critType == CreatureTemplate.Type.Slugcat)
                symbol.RemoveSprites();
        }

        //step 4: draw those avatars!
        foreach (var avatar in avatarList)
        {
            var player = avatar.player;
            //if (player == owner.abstractCreature) continue; //redundant check

            //if (player.realizedCreature is not Player realPlayer) continue;
            if (!player.pos.TileDefined) //player doesn't even have a tile location???
            {
                Logger.LogWarning("No tile position for player!!! " + player.ToString());
                continue;
            }

            //create symbol
            var symbol = new CreatureSymbol(CreatureSymbol.SymbolDataFromCreature(player), self.inFrontContainer);
            symbol.Show(true);
            symbol.lastShowFlash = 0f;
            symbol.showFlash = 0f;

            //set player colors here???
            symbol.myColor = avatar.color;
            symbol.symbolSprite.alpha = 0.9f;

            //shrink dead or indeterminate players (probably distant ones)
            if (player.realizedCreature is null || player.realizedCreature.dead)
            {
                symbol.symbolSprite.scale = 0.8f;
                symbol.symbolSprite.alpha = 0.7f;
            }

            //modify shadows
            symbol.shadowSprite1.alpha = symbol.symbolSprite.alpha;
            symbol.shadowSprite2.alpha = symbol.symbolSprite.alpha;
            symbol.shadowSprite1.scale = symbol.symbolSprite.scale;
            symbol.shadowSprite2.scale = symbol.symbolSprite.scale;

            //draw in correct position
            Vector2 drawPos = self.RoomToMapPos((player.realizedCreature is null) ? player.pos.Tile.ToVector2() * 20f : player.realizedCreature.mainBodyChunk.pos, player.Room.index, timeStacker);
            symbol.Draw(timeStacker, drawPos);

            //add to creatureSymbol list to get cleared!
            self.creatureSymbols.Add(symbol);
        }

        avatarList.Clear();
    }
    #endregion
}
