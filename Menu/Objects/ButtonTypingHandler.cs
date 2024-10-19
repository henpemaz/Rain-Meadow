using System.Collections.Generic;
using UnityEngine;
using Menu.Remix.MixedUI;
using System;

namespace RainMeadow
{
    public class ButtonTypingHandler : TypingHandler
    {
        public string _lastInput = "";
        public HashSet<ICanBeTyped> _assigned = new HashSet<ICanBeTyped>();
        public ICanBeTyped _focused;
        public void Update()
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
            while (queue.Count > 0 && _focused.OnKeyDown != null)
            {
                _focused?.OnKeyDown(queue.Dequeue()); // TODO fix this guy later its what causes text typing without the chattextbox
            }
            _lastInput = inputString;
        }

        public void OnDestroy()
        {
            _HandlerOnDestroy();
        }

        private void _HandlerOnDestroy()
        {
            _assigned.Clear();
            _focused = null;
            CanBeTypedExt._HandlerOnDestroy();
        }

        public void Assign(ICanBeTyped typable)
        {
            _assigned.Add(typable);
        }
        public bool IsAssigned(ICanBeTyped typable)
        {
            return _assigned.Contains(typable);
        }
        public void Unassign(ICanBeTyped typable)
        {
            RainMeadow.Debug("Unassigning chatbox");
            _assigned.Clear();
            _assigned.Remove(typable);
        }
    }
}