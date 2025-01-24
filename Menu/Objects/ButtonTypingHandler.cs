using System.Collections.Generic;
using UnityEngine;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    public class ButtonTypingHandler : TypingHandler
    {
        public new string _lastInput = "";
        public new HashSet<ICanBeTyped> _assigned = new HashSet<ICanBeTyped>();
        public new ICanBeTyped? _focused;
        public new void Update()
        {
            if (_assigned.Count < 1)
            {
                Destroy(base.gameObject);
                return;
            }
            if (_focused != null)
            {
                _focused = null;
            }
            if (_focused == null)
            {
                foreach (ICanBeTyped canBeTyped in _assigned)
                {
                    _focused = canBeTyped;
                    break;
                }
                if (_focused == null)
                {
                    _lastInput = "";
                    return;
                }
            }
            string inputString = Input.inputString;
            HashSet<char> hashSet = new HashSet<char>();
            for (int i = 0; i < _lastInput.Length; i++)
            {
                hashSet.Add(_lastInput[i]);
            }
            Queue<char> queue = new Queue<char>();
            for (int j = 0; j < inputString.Length; j++)
            {
                if (!hashSet.Contains(inputString[j]))
                {
                    queue.Enqueue(inputString[j]);
                }
            }
            while (queue.Count > 0)
            {
                _focused?.OnKeyDown(queue.Dequeue());
            }
            _lastInput = inputString;
        }
        public new void OnDestroy()
        {
            _assigned.Clear();
            _focused = null;
            CanBeTypedExt._HandlerOnDestroy();
        }
        public new void Assign(ICanBeTyped typable)
        {
            _assigned.Add(typable);
        }
        public new void Unassign(ICanBeTyped typable)
        {
            _assigned.Clear();
            _assigned.Remove(typable);
        }
    }
}