using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace QTE.Editor
{
    public class QTESearchWindow : ScriptableObject, ISearchWindowProvider
    {
        QTEGraphView _view;
        Vector2 _graphMouse;

        public void Init(QTEGraphView view)
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
                Entry("Start", QTENodeKind.Start),
                Entry("Wait", QTENodeKind.Wait),
                Entry("Delay", QTENodeKind.Delay),
                Entry("Action", QTENodeKind.Action),
                Entry("InputPrompt", QTENodeKind.InputPrompt),
                Entry("Hold", QTENodeKind.Hold),
                Entry("Mash", QTENodeKind.Mash),
                Entry("SequenceInput", QTENodeKind.SequenceInput),
                Entry("Sequence", QTENodeKind.Sequence),
                Entry("Branch", QTENodeKind.Branch),
                Entry("End", QTENodeKind.End)
            };
        }

        static SearchTreeEntry Entry(string label, QTENodeKind kind)
        {
            return new SearchTreeEntry(new GUIContent(label)) { level = 1, userData = kind };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_view == null || searchTreeEntry.userData is not QTENodeKind kind)
                return false;

            _view.CreateNode(kind, _graphMouse);
            return true;
        }
    }
}
