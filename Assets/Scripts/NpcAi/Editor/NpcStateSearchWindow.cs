using System.Collections.Generic;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NpcAi.Editor
{
    public class NpcStateSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        NpcStateGraphView _view;
        Vector2 _graphMouse;

        public void Init(NpcStateGraphView view)
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
                Entry("Start", NpcStateNodeKind.Start),
                Entry("Wait", NpcStateNodeKind.Wait),
                Entry("Character Action", NpcStateNodeKind.CharacterAction),
                Entry("Scene Action", NpcStateNodeKind.SceneAction),
                Entry("Wait Event", NpcStateNodeKind.WaitEvent),
                Entry("Random Branch", NpcStateNodeKind.RandomBranch),
                Entry("End", NpcStateNodeKind.End)
            };
        }

        static SearchTreeEntry Entry(string label, NpcStateNodeKind kind)
        {
            return new SearchTreeEntry(new GUIContent(label)) { level = 1, userData = kind };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not NpcStateNodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
