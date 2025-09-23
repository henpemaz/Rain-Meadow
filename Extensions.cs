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
        public static int RealModulo(int dividend, int divisor)
        {
            int remainder = dividend % divisor;               //C#'s % is not a modulo operator, it's a *remainder*, which will return negative values if the dividend is negative. That's stupid.
            return remainder + (remainder < 0 ? divisor : 0); //C# also doesn't have a Math.Mod() function to fill the hole created by % not being a true modulo. That's even stupider.
        }
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

        public static void MoveMovable(this AbstractPhysicalObject apo, WorldCoordinate newCoord) {
            foreach (AbstractPhysicalObject obj in apo.GetAllConnectedObjects()) {
                if (obj.CanMove(newCoord, true))
                {   
                    if (newCoord.CompareDisregardingTile(obj.pos)) return;

                    obj.timeSpentHere = 0;
                    if (newCoord.room != obj.pos.room)
                    {
                        obj.ChangeRooms(newCoord);
                    }

                    if (!newCoord.TileDefined && obj.pos.room == newCoord.room)
                    {
                        newCoord.Tile = obj.pos.Tile;
                    }

                    obj.pos = newCoord;
                    obj.world.GetResource().ApoEnteringWorld(obj);
                    obj.world.GetAbstractRoom(newCoord.room).GetResource()?.ApoEnteringRoom(obj, newCoord);
                } 
            }
        }
        public static void MoveOnly(this AbstractPhysicalObject apo, WorldCoordinate newCoord) {
            if (apo.CanMove(newCoord, true)) {
                if (newCoord.CompareDisregardingTile(apo.pos)) return;

                apo.timeSpentHere = 0;
                if (newCoord.room != apo.pos.room)
                {
                    try {
                        apo.ChangeRooms(newCoord);
                    } catch (Exception except) {
                        RainMeadow.Error(except);
                        RainMeadow.Debug("Manually setting room");
                        apo.world?.GetAbstractRoom(apo.pos)?.RemoveEntity(apo);
                        apo.world?.GetAbstractRoom(newCoord)?.AddEntity(apo);
                    }
                    
                }

                if (!newCoord.TileDefined && apo.pos.room == newCoord.room)
                {
                    newCoord.Tile = apo.pos.Tile;
                }

                apo.pos = newCoord;
                apo.world.GetResource().ApoEnteringWorld(apo);
                apo.world.GetAbstractRoom(newCoord.room).GetResource()?.ApoEnteringRoom(apo, newCoord);
            }
        }

        public static bool RemoveFromShortcuts<T>(ref List<T> vessels, Creature creature, AbstractRoom? room = null) where T : ShortcutHandler.Vessel 
        {
            bool removefromallrooms = room is null;
            for (int i = 0; i < vessels.Count; i++)
            {
                if (vessels[i].creature == creature && ((vessels[i].room == room) || removefromallrooms))
                {
                    vessels.RemoveAt(i);
                    creature.inShortcut = false;
                    return true;
                }
            }
            return false;
        }
        public static bool RemoveFromShortcuts(this Creature creature, AbstractRoom? room = null)
        {
            if (!creature.inShortcut) return true;
            var handler = creature.abstractCreature.world.game.shortcuts;
            bool found = false;
            if (RemoveFromShortcuts(ref handler.transportVessels, creature, room)) found = true;
            if (RemoveFromShortcuts(ref handler.borderTravelVessels, creature, room)) found = true;
            if (RemoveFromShortcuts(ref handler.betweenRoomsWaitingLobby, creature, room)) found = true;
            
            if (!found) RainMeadow.Debug("not found");
            return found;
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
        //I've been told these could go in Menu/MenuHelpers.cs, but separating them from the functions they're based off of seems wrong. If someone else wants to though, go for it.
        ///<summary>Effectively deletes a UI element's directional keybind(s) by binding the button to itself.</summary>
        public static void TryDeleteBind(MenuObject menuObject, bool left = false, bool right = false, bool top = false, bool bottom = false)
        {
            menuObject.nextSelectable[0] = (left ? menuObject : menuObject.nextSelectable[0]);
            menuObject.nextSelectable[1] = (top ? menuObject : menuObject.nextSelectable[1]);
            menuObject.nextSelectable[2] = (right ? menuObject : menuObject.nextSelectable[2]);
            menuObject.nextSelectable[3] = (bottom ? menuObject : menuObject.nextSelectable[3]);
        }
        ///<summary>Directionally binds a list of UI elements to a target element. For example, with fromObjects[A,B,C] and ToObject D, Bind A→D, B→D, and C→D.</summary>
        public static void TryMassBind(List<MenuObject> fromObjects, MenuObject toObject, bool left = false, bool right = false, bool top = false, bool bottom = false)
        {
            foreach (MenuObject FromObject in fromObjects)
            {
                TryBind(FromObject, toObject, left, right, top, bottom);
            }
        }
        ///<summary>Effectively deletes a UI list of elements' directional keybind(s) by binding each button to itself.</summary>
        public static void TryMassDeleteBind(List<MenuObject> objects, bool left = false, bool right = false, bool top = false, bool bottom = false)
        {
            foreach (MenuObject Object in objects)
            {
                TryDeleteBind(Object, left, right, top, bottom);
            }
        }
        ///<summary>Chains MutualBinds together from a list. For example, with menuObjects[A,B,C,D], MutualBind A↔B, B↔C, C↔D, and optionally D↔A. Rain World handles MutualBinds from BOTTOM TO TOP, or left to right. Use reverseList if you need.</summary>
        public static void TrySequentialMutualBind(this Menu.Menu menu, List<MenuObject> menuObjects, bool leftRight = false, bool bottomTop = false, bool loopLastIndex = false, bool reverseList = false)
        {
            List<MenuObject> WorkingObjects = menuObjects.Where(MenuObject => MenuObject != null).ToList(); //If our input list contains null entries (such as uninitialized), just remove them and continue gracefully.
            if (WorkingObjects.Count < 2)
            {
                RainMeadow.Warn(" Tried to keybind " + WorkingObjects.Count + " UI element(s) to each other, cancelling operation. Is the list not yet populated?");
                return;
            }
            if (reverseList)
            {
                WorkingObjects.Reverse();
            }
            for (int i=0; i < WorkingObjects.Count - 1; i++)
            {
                TryMutualBind(menu, WorkingObjects[i], WorkingObjects[i+1], leftRight, bottomTop);
            }
            if (loopLastIndex)
            {
                TryMutualBind(menu, WorkingObjects[WorkingObjects.Count - 1], WorkingObjects[0], leftRight, bottomTop);
            }
        }
        /// <summary>Mutually binds two different lists of elements together based on their mathematical relative positions; designed for handling parallel rows or columns with equal length, but different numbers of elements. Rain World handles MutualBinds from BOTTOM TO TOP, or left to right, use swapLists if you want the readability.</summary>
        public static void TryParallelStitchBind(List<MenuObject> fromObjectsList, List<MenuObject> toObjectsList, bool areRows = false, bool areColumns = false, bool swapLists = false, bool reverseFromList = false, bool reverseToList = false)
        {
            //Clean up the lists
            List<MenuObject> ListA = fromObjectsList.Where(MenuObject => MenuObject != null).ToList(); //If our input lists contain null entries (such as uninitialized), just remove them and continue as if they don't exist.
            List<MenuObject> ListB =   toObjectsList.Where(MenuObject => MenuObject != null).ToList();
            if (ListA.Count < 1) { RainMeadow.Warn(" Tried to UI keybind to an empty or null fromObjects, cancelling operation. Is the list not yet populated?"); return; }
            if (ListB.Count < 1) { RainMeadow.Warn(" Tried to UI keybind to an empty or null toObjects, cancelling operation. Is the list not yet populated?");   return; }
            if (reverseFromList) { ListA.Reverse(); }
            if (reverseToList)   { ListB.Reverse(); }
            if (swapLists) { (ListA, ListB) = (ListB, ListA); }
            //Create 2 button pointers, one for each list
            int NotSoLeastCommonMultiple = (ListA.Count - 1) * (ListB.Count - 1);
            int ListAStepper = 0;
            int ListBStepper = 0;
            if (ListA.Count > 1 && ListB.Count > 1)
            {
                while (ListAStepper <= NotSoLeastCommonMultiple || ListBStepper <= NotSoLeastCommonMultiple)
                {
                    //At the lower pointer, compare the distance to the higher pointer's current button and the one before it, then increment the lower pointer to the next button.
                    if (ListAStepper <= ListBStepper)
                    {
                        ParallelStitchBindFindClosest(ListA, ListB, ListAStepper, ListBStepper, areRows, areColumns, isTempSwap: false);
                        ListAStepper += (ListB.Count - 1);
                    }
                    else
                    {
                        ParallelStitchBindFindClosest(ListB, ListA, ListBStepper, ListAStepper, areRows, areColumns, isTempSwap: true);
                        ListBStepper += (ListA.Count - 1);
                    }
                }
            }
            //1-length lists are evil and create a catch-22 between "infinite while loop" and "OOB causing misbinds". So, gaslight the second step into thinking its a TryMassBind with extra directional logic.
            else
            {
                foreach (MenuObject fromObject in ListA)
                {
                    ParallelStitchBindFindClosest(new List<MenuObject>() { fromObject }, new List<MenuObject>() { ListB[0] }, 0, 0, areRows, areColumns, false);
                }
                foreach (MenuObject fromObject in ListB)
                {
                    ParallelStitchBindFindClosest(new List<MenuObject>() { fromObject }, new List<MenuObject>() { ListA[0] }, 0, 0, areRows, areColumns, true);
                }
            }
        }
        private static void ParallelStitchBindFindClosest(List<MenuObject> ListA, List<MenuObject> ListB, int ListAStepper, int ListBStepper, bool areRows, bool areColumns, bool isTempSwap)
        {
            //Get the scaled index of whichever button is closer. Button "positions" are basically a 0%-100% through the list, but stored as an integer because it's clean and I don't even want to bother with floats.
            int TargetStep = ListBStepper;
            if (ListAStepper - (ListBStepper - (ListA.Count - 1)) <= ListBStepper - ListAStepper)
            {
                TargetStep -= (ListA.Count - 1);
            }
            //Find which direction we need to bind, then bind. Referencing the index of a division is conceptually sketchy, but the math checks out and has been throughly tested. Worst-case scenario it's integer division anyway.
            TryBind(
                ListA[ListAStepper / Math.Max(1, ListB.Count - 1)],
                ListB[TargetStep   / Math.Max(1, ListA.Count - 1)],
                top:   !isTempSwap && areRows,
                bottom: isTempSwap && areRows,
                right: !isTempSwap && areColumns,
                left:   isTempSwap && areColumns
            );
        }
        /// <summary>Hybrid of TrySeqentualMutualBind() and TryParallelStitchBind(); find the best way to bind a list of lists together. Rain World handles MutualBinds from BOTTOM TO TOP, or left to right, use reverseListList if you want the readability.</summary>
        public static void TrySequentialParallelStitchBind(List<List<MenuObject>> listList, bool areRows = false, bool areColumns = false, bool loopLastIndex = false, bool reverseListList = false)
        {
            List<List<MenuObject>> WorkingLists = listList.Where(List => List != null).ToList(); //If our input list contains null entries (such as uninitialized), just remove them and continue gracefully.
            if (WorkingLists.Count < 2)
            {
                RainMeadow.Warn(" Tried to keybind stitch " + listList.Count + " UI element list(s) to each other, cancelling operation. Is the list not yet populated?");
                return;
            }
            if (reverseListList)
            {
                WorkingLists.Reverse();
            }
            for (int i = 0; i < listList.Count - 1; i++)
            {
                TryParallelStitchBind(listList[i + 1], listList[i], areRows, areColumns);
            }
            if (loopLastIndex)
            {
                TryParallelStitchBind(listList[0], listList[listList.Count - 1], areRows, areColumns);
            }
        }
    }
}
