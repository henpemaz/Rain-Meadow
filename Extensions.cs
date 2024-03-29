﻿using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public static class Extensions
    {
        public static WorldSession GetResource(this World world)
        {
            return WorldSession.map.GetValue(world, (w) => throw new KeyNotFoundException($"World {w.name} not found"));
        }

        public static RoomSession GetResource(this AbstractRoom room)
        {
            return RoomSession.map.GetValue(room, (r) => throw new KeyNotFoundException($"Room {r.name} not found"));
        }

        public static OnlinePhysicalObject GetOnlineObject(this AbstractPhysicalObject apo)
        {
            return OnlinePhysicalObject.map.GetValue(apo, (apo) => throw new KeyNotFoundException($"Object {apo} not found"));
        }

        public static OnlineCreature GetOnlineCreature(this AbstractCreature ac)
        {
            return (OnlineCreature)OnlineCreature.map.GetValue(ac, (ac) => throw new KeyNotFoundException($"Creature{ac} not found"));
        }

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

        public static bool CloseEnoughZeroSnap(this Vector2 a, Vector2 b, float tolerance)
        {
            if (a == b) return true;
            if (a.x == 0 && a.y == 0) return false; // zero and non-zero situation!
            if (b.x == 0 && b.y == 0) return false;
            return (a - b).sqrMagnitude < tolerance * tolerance;
        }

        public static bool CloseEnough(this Vector2 a, Vector2 b, float tolerance)
        {
            return a == b || (a - b).sqrMagnitude < tolerance * tolerance;
        }

        public static HSLColor ToHSL(this Color c)
        {
            var cv = Custom.RGB2HSL(c);
            return new HSLColor(cv[0], cv[1], cv[2]);
        }

        // copied from futile but tweaked for accurate result on non-zero-centered rect
        public static Vector2 GetClosestInteriorPointAlongLineFromCenter(this Rect rect, Vector2 targetPoint)
        {
            //if it's inside the rect, don't do anything
            if (targetPoint.x >= rect.xMin &&
                targetPoint.x <= rect.xMax &&
                targetPoint.y >= rect.yMin &&
                targetPoint.y <= rect.yMax) return targetPoint;

            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;
            targetPoint -= rect.center; // from center
            targetPoint.Normalize();

            float absX = Mathf.Abs(targetPoint.x);
            float absY = Mathf.Abs(targetPoint.y);

            if (halfWidth * absY <= halfHeight * absX)
            {
                return targetPoint * halfWidth / absX + rect.center;
            }
            else
            {
                return targetPoint * halfHeight / absY + rect.center;
            }
        }
    }
}