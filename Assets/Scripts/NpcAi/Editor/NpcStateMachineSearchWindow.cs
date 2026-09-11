using System.Collections.Generic;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NpcAi.Editor
{
    public class NpcStateMachineSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        NpcStateMachineGraphView _view;
        Vector2 _graphMouse;

        public void Init(NpcStateMachineGraphView view)
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
                Entry("Start", NpcStateMachineNodeKind.Start),
                Entry("State", NpcStateMachineNodeKind.State),
                Entry("Action", NpcStateMachineNodeKind.Action),
                Entry("End", NpcStateMachineNodeKind.End)
            };
        }

        static SearchTreeEntry Entry(string label, NpcStateMachineNodeKind kind)
        {
            return new SearchTreeEntry(new GUIContent(label)) { level = 1, userData = kind };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not NpcStateMachineNodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
