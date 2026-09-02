using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Dialogue.Editor
{
    public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        DialogueGraphView _view;
        Vector2 _graphMouse;

        public void Init(DialogueGraphView view)
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
                new SearchTreeEntry(new GUIContent("Line")) { level = 1, userData = DialogueNodeKind.Line },
                new SearchTreeEntry(new GUIContent("Choice")) { level = 1, userData = DialogueNodeKind.Choice },
                new SearchTreeEntry(new GUIContent("End")) { level = 1, userData = DialogueNodeKind.End },
                new SearchTreeEntry(new GUIContent("Start")) { level = 1, userData = DialogueNodeKind.Start }
            };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not DialogueNodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
