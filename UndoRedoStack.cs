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

        public bool Undo(List<System.Drawing.Point> visitedPoints)
        {
            bool invoke = false;
            if (undoStack.Count > 0)
            {
                if (visitedPoints.Count != 0) visitedPoints.Clear();
                undoStack.Pop().Invoke();
                invoke = true;
            }
            return invoke;
        }

        public bool Redo(List<System.Drawing.Point> visitedPoints)
        {
            bool invoke = false;
            if (redoStack.Count > 0)
            {
                if (visitedPoints.Count != 0) visitedPoints.Clear();
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
