﻿using System.Collections.Generic;

namespace RainMeadow
{
    public static class Extensions
    {

        public static bool RemoveFromShortcuts(this Creature creature)
        {
            if (!creature.inShortcut) return true;
            var handler = creature.abstractCreature.world.game.shortcuts;
            var allConnectedObjects = creature.abstractCreature.GetAllConnectedObjects();
            for (int j = 0; j < allConnectedObjects.Count; j++)
            {
                if (allConnectedObjects[j].realizedObject is Creature other && other != creature)
                {
                    return false; // can't be removed because connected to other
                }
            }
            for (int i = 0; i < handler.transportVessels.Count; i++)
            {
                if (handler.transportVessels[i].creature == creature)
                {
                    handler.transportVessels.RemoveAt(i);
                    creature.inShortcut = false;
                    return true;
                }
            }
            for (int i = 0; i < handler.borderTravelVessels.Count; i++)
            {
                if (handler.borderTravelVessels[i].creature == creature)
                {
                    handler.borderTravelVessels.RemoveAt(i);
                    creature.inShortcut = false;
                    return true;
                }
            }
            for (int i = 0; i < handler.betweenRoomsWaitingLobby.Count; i++)
            {
                if (handler.betweenRoomsWaitingLobby[i].creature == creature)
                {
                    handler.betweenRoomsWaitingLobby.RemoveAt(i);
                    creature.inShortcut = false;
                    return true;
                }
            }
            return false; // not found??
        }

        // suck it, linq
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this IEnumerable<KeyValuePair<TKey, TElement>> source)
        {
            Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>();
            foreach (KeyValuePair<TKey, TElement> item in source) // really though, you'd think there would be something like AddRange, but nah
            {
                dictionary.Add(item.Key, item.Value);
            }

            return dictionary;
        }
    }
}