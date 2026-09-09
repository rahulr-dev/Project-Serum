using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace InteractionSystem.Editor
{
    public class InteractionSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        InteractionGraphView _view;
        Vector2 _graphMouse;

        public void Init(InteractionGraphView view)
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
                Entry("Start", InteractionNodeKind.Start),
                Entry("Wait", InteractionNodeKind.Wait),
                Entry("Action", InteractionNodeKind.Action),
                Entry("End", InteractionNodeKind.End)
            };
        }

        static SearchTreeEntry Entry(string label, InteractionNodeKind kind)
        {
            return new SearchTreeEntry(new GUIContent(label)) { level = 1, userData = kind };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not InteractionNodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
