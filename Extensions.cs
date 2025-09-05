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
        ///<summary>Directionally binds a list of UI elements to a target element. For example, with fromObjects[A,B,C] and ToObject D, Bind A→D, B→D, and C→D.</summary>
        public static void TryMassBind(List<MenuObject> fromObjects, MenuObject toObject, bool left = false, bool right = false, bool top = false, bool bottom = false)
        {
            foreach (MenuObject FromObject in fromObjects)
            {
                TryBind(FromObject, toObject, left, right, top, bottom);
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
        /// <summary>Mutually binds two different lists of elements together based on their UI position, designed for handling parallel rows or columns with potentially-unequal length.</summary>
        public static void TryParallelStitchBind(List<MenuObject> objectsListA, List<MenuObject> objectsListB, bool areRows = false, bool areColumns = false, bool reverseFromList = false, bool reverseToList = false)
        {
            List<MenuObject> ListA = objectsListA.Where(MenuObject => MenuObject != null).ToList(); //If our input lists contain null entries (such as uninitialized), just remove them and continue as if they don't exist.
            List<MenuObject> ListB = objectsListB.Where(MenuObject => MenuObject != null).ToList();
            if (ListA.Count < 1) { RainMeadow.Warn(" Tried to keybind to an empty or null fromObjects, cancelling operation. Is the list not yet populated?"); return; }
            if (ListB.Count < 1) { RainMeadow.Warn(" Tried to keybind to an empty or null toObjects, cancelling operation. Is the list not yet populated?");   return; }
            if (reverseFromList) { ListA.Reverse(); }
            if (reverseToList  ) { ListB.Reverse(); }






            //int NotSoLeastCommonMultiple = ListA.Count * ListB.Count;
            //int ListAStepper = 0;
            //int ListBStepper = 0;
            //RainMeadow.Debug(ListAStepper + " " + ListBStepper + " - " + NotSoLeastCommonMultiple);
            //while (ListAStepper < NotSoLeastCommonMultiple || ListBStepper < NotSoLeastCommonMultiple)
            //{
            //    if (ListAStepper <= ListBStepper)
            //    {
            //        ListAStepper += ListB.Count;
            //    }
            //    else
            //    {
            //        ListBStepper += ListA.Count;
            //    }
            //    RainMeadow.Debug(ListAStepper + " " + ListBStepper);
            //}

            //if ((ListBStepper - ListAStepper) <= ((ListBStepper + ListB.Count) - ListAStepper))
            //{
            //    RainMeadow.Debug("Binding 0[" + ListAStepper / ListA.Count + "] to 1[" + ListBStepper / ListB.Count + "]");
            //    //TryBind(ListA[ListAStepper / ListA.Count], ListB[ListBStepper / ListB.Count]);
            //}
            //else
            //{
            //    RainMeadow.Debug("Binding 0[" + ListAStepper / ListA.Count + "] to 1[" + (ListBStepper / ListB.Count) + 1 + "]");
            //    //TryBind(fromObjects[ListAStepper / ListA.Count], ListB[(ListBStepper / ListB.Count)+1]);
            //}

            //if ((ListAStepper - ListBStepper) <= ((ListAStepper + ListA.Count) - ListBStepper))
            //{
            //    RainMeadow.Debug("Binding 1[" + ListBStepper / ListB.Count + "] to 0[" + ListAStepper / ListA.Count + "]");
            //    //TryBind(ListB[ListBStepper / ListB.Count], ListA[ListAStepper / ListA.Count]);
            //}
            //else
            //{
            //    RainMeadow.Debug("Binding 1[" + ListBStepper / ListB.Count + "] to 0[" + (ListAStepper / ListA.Count) + 1 + "]");
            //    //TryBind(ListB[ListBStepper / ListB.Count], ListA[(ListAStepper / ListA.Count)+1]);
            //}














            //int BestTargetID = 0;
            //float BestTargetVector = float.PositiveInfinity;
            //foreach (MenuObject CurrentObject in ListA)
            //{
            //    RainMeadow.Debug(CurrentObject.Container.GetPosition());
            //}
        }
    }
}
