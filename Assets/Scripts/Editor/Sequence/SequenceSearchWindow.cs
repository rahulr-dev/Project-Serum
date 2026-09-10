using System.Collections.Generic;
using SequenceSystem;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SequenceSystem.Editor
{
    public class SequenceSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        SequenceGraphView _view;
        Vector2 _graphMouse;

        public void Init(SequenceGraphView view)
        {
            _view = view;
        }

        public void SetGraphMouse(Vector2 graphMouse)
        {
            _graphMouse = graphMouse;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                Entry("Start", SequenceNodeKind.Start),
                Entry("Action", SequenceNodeKind.Action),
                Entry("Condition", SequenceNodeKind.Condition),
                Entry("Clear Sequence", SequenceNodeKind.ClearSequence),
                Entry("End", SequenceNodeKind.End)
            };
        }

        static SearchTreeEntry Entry(string label, SequenceNodeKind kind)
        {
            return new SearchTreeEntry(new GUIContent(label)) { level = 1, userData = kind };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not SequenceNodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
