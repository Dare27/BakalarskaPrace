using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakalarskaPrace
{
    internal class UndoRedoStack
    {
        public Stack<Action> undoStack = new Stack<Action>();
        public Stack<Action> redoStack = new Stack<Action>();

        public void AddActionToUndo(Action action, bool clearRedoStack)
        {
            undoStack.Push(action);
            if (clearRedoStack == true) redoStack.Clear();
        }

        public void AddActionToRedo(Action action)
        {
            redoStack.Push(action);
        }

        public bool Undo()
        {
            bool invoke = false;
            if (undoStack.Count > 0)
            {
                undoStack.Pop().Invoke();
                invoke = true;
            }
            return invoke;
        }

        public bool Redo()
        {
            bool invoke = false;
            if (redoStack.Count > 0)
            {
                redoStack.Pop().Invoke();
                invoke = true;
            }
            return invoke;
        }

        public void ClearStacks() 
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
