using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RainMeadow;
using UnityEngine;
using static ShowPlayersEverywhere.Mod;

namespace ShowPlayersEverywhere;

public class MeadowCompat
{
    public static bool IsOnline => OnlineManager.lobby != null;

    /// <summary>
    /// Loops through the lobby's list of player avatars (online players, basically) EXCEPT myself.
    /// This function is pretty inefficient for being run every frame, but it seems to work perfectly well.
    /// </summary>
    /// <returns>A list of abstractCreatures (who are players) and their base colors (or white if unfound).</returns>
    public static List<PlayerAvatarData> GetAvatarData()
    {
        List<PlayerAvatarData> list = new();
        foreach (var avatar in OnlineManager.lobby.playerAvatars) {
            //exclude myself
            if (!avatar.Key.isMe && avatar.Value.FindEntity(true) is OnlineCreature entity)
                list.Add(new(entity.abstractCreature, entity.TryGetData<SlugcatCustomization>(out var data) ? data.bodyColor : Color.white));
        }
        return list;
    }
}
