using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public static class Extensions
    {
        public static Color ColorFromHex(int value)
        {
            return new Color(((value >> 16) & 0xff) / 255f, ((value >> 8) & 0xff) / 255f, (value & 0xff) / 255f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WorldSession? GetResource(this World world)
        {
            return WorldSession.map.TryGetValue(world, out var ws) ? ws : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RoomSession? GetResource(this AbstractRoom room)
        {
            return RoomSession.map.TryGetValue(room, out var rs) ? rs : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OnlinePhysicalObject? GetOnlineObject(this AbstractPhysicalObject apo)
        {
            return OnlinePhysicalObject.map.TryGetValue(apo, out var oe) ? oe : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetOnlineObject(this AbstractPhysicalObject apo, out OnlinePhysicalObject? opo) => OnlinePhysicalObject.map.TryGetValue(apo, out opo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OnlineCreature? GetOnlineCreature(this AbstractCreature ac) => GetOnlineObject(ac) as OnlineCreature;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetOnlineCreature(this AbstractCreature apo, out OnlineCreature? oc) => (oc = GetOnlineCreature(apo)) is not null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocal(this AbstractPhysicalObject apo) => OnlineManager.lobby is null || (GetOnlineObject(apo)?.isMine ?? true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocal(this AbstractPhysicalObject apo, out OnlinePhysicalObject? opo)
        {
            opo = null;
            return OnlineManager.lobby is null || (OnlinePhysicalObject.map.TryGetValue(apo, out opo) && opo.isMine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocal(this PhysicalObject po) => IsLocal(po.abstractPhysicalObject);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocal(this PhysicalObject po, out OnlinePhysicalObject? opo) => IsLocal(po.abstractPhysicalObject, out opo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanMove(this AbstractPhysicalObject apo, WorldCoordinate? newCoord=null, bool quiet=false)
        {
            if (!GetOnlineObject(apo, out var oe)) return true;
            if (!oe.isMine && !oe.beingMoved && (newCoord is null || oe.roomSession is null || oe.roomSession.absroom.index != newCoord.Value.room))
            {
                if (!quiet) RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                return false;
            }
            return true;
        }

        public static bool RemoveFromShortcuts(this Creature creature)
        {
            if (!creature.inShortcut) return true;
            var handler = creature.abstractCreature.world.game.shortcuts;
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
            RainMeadow.Debug("not found");
            return false;
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

        public static (List<T1>, List<T2>) ToListTuple<T1, T2>(this IEnumerable<(T1, T2)> source)
        {
            var list = source.ToList(); // eval once
            var listA = new List<T1>(list.Count);
            var listB = new List<T2>(list.Count);
            foreach (var t in list)
            {
                listA.Add(t.Item1);
                listB.Add(t.Item2);
            }

            return (listA, listB);
        }

        // making linq better one extension at a time
        public static T MinBy<T>(this IEnumerable<T> source, Func<T, float> evaluator)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            float num = 0f;
            bool init = false;
            T best = default(T);
            foreach (T item in source)
            {
                var val = evaluator(item);
                if (init)
                {
                    if (!init || val < num || float.IsNaN(val))
                    {
                        num = val;
                        best = item;
                    }
                }
                else
                {
                    num = val;
                    best = item;
                    init = true;
                }
            }

            if (init)
            {
                return best;
            }

            throw new ArgumentException("no elements in sequence");
        }
        public static T? GetValueOrDefault<T>(this IList<T> iList, int index)
        {
            return iList.GetValueOrDefault(index, default);
        }

        public static T? GetValueOrDefault<T>(this IList<T> iList, int index, T? defaultVal)
        {
            return iList != null && index >= 0 && iList.Count > index? iList[index] : defaultVal;
        }

        public static bool CloseEnoughZeroSnap(this Vector2 a, Vector2 b, float sqrltol)
        {
            if (a == b) return true;
            if (a.x == 0 && a.y == 0) return false; // zero and non-zero situation!
            if (b.x == 0 && b.y == 0) return false;
            return (a - b).sqrMagnitude < sqrltol;
        }

        public static bool CloseEnough(this Vector2 a, Vector2 b, float sqrtol)
        {
            return a == b || (a - b).sqrMagnitude < sqrtol;
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

        public static Color SafeColorRange(this Color valuecolor)
        {
            return new Color(Mathf.Clamp(valuecolor.r, 1f / 255f, 1f), Mathf.Clamp(valuecolor.g, 1f / 255f, 1f), Mathf.Clamp(valuecolor.b, 1f / 255f, 1f));
        }

        public static Type[] GetTypesSafely(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e) // happens often with soft-dependencies, did you know
            {
                return e.Types.Where(x => x != null).ToArray();
            }
        }
        //faster for adding menuobjects smth
        public static void ClearMenuObjectIList<T>(this MenuObject owner, IEnumerable<T>? menuObjects) where T : MenuObject
        {
            if (menuObjects != null)
            {
                foreach (MenuObject menuObject in menuObjects)
                {
                    owner.ClearMenuObject(menuObject);
                }
            }
        }
        public static void ClearMenuObject<T>(this MenuObject owner, ref T? subObject) where T : MenuObject
        {
            owner.ClearMenuObject(subObject);
            subObject = null;

        }
        public static void ClearMenuObject(this MenuObject owner, MenuObject? subObject)
        {
            if (subObject != null)
            {
                subObject.RemoveSprites();
                owner.RemoveSubObject(subObject);
            }

        }
        public static void TryBind(this MenuObject? menuObject, MenuObject? bindWith, bool left = false, bool right = false, bool top = false, bool bottom = false)
        {
            if (menuObject != null && bindWith != null)
            {
                menuObject.nextSelectable[0] = (left ? bindWith : menuObject.nextSelectable[0]);
                menuObject.nextSelectable[1] = (top ? bindWith : menuObject.nextSelectable[1]);
                menuObject.nextSelectable[2] = (right ? bindWith : menuObject.nextSelectable[2]);
                menuObject.nextSelectable[3] = (bottom ? bindWith : menuObject.nextSelectable[3]);
            }
        }
        public static void TryMutualBind(this Menu.Menu? menu, MenuObject? first, MenuObject? second, bool leftRight = false, bool bottomTop = false)
        {
            if (menu != null && first != null && second != null)
            {
                if (leftRight)
                {
                    menu.MutualHorizontalButtonBind(first, second);
                }
                if (bottomTop)
                {
                    menu.MutualVerticalButtonBind(first, second);
                }
            }
        }
    }
}
