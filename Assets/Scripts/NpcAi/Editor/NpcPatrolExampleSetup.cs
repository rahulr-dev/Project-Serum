using Character;
using Events;
using NpcAi;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NpcAi.Editor
{
    public static class NpcPatrolExampleSetup
    {
        const string GuardName = "PatrolGuard";
        const string StatesFolder = "Assets/Data/NpcStates";
        const string MachinesFolder = "Assets/Data/NpcStateMachines";
        const string PatrolPath = StatesFolder + "/NpcState_Patrol.asset";
        const string SearchPath = StatesFolder + "/NpcState_Search.asset";
        const string BrainPath = MachinesFolder + "/NpcBrain_PatrolSearch.asset";

        [MenuItem("Serum/Setup Patrol Search Example (Open Scene)", false, 26)]
        public static void Setup()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(StatesFolder);
            EnsureFolder(MachinesFolder);

            string torchHandlerId;
            NpcStateGraph patrol = LoadOrCreatePatrol();
            NpcStateGraph search = LoadOrCreateSearch(out torchHandlerId);
            NpcStateMachineGraph brain = LoadOrCreateBrain(patrol, search);

            GameObject existing = GameObject.Find(GuardName);
            if (existing != null)
            {
                UpgradeExistingGuard(existing, brain, patrol, torchHandlerId);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Selection.activeGameObject = existing;
                Debug.Log("[NpcAi] PatrolGuard upgraded: NPC locomotion, spotlight, and cone trigger.", existing);
                return;
            }

            GameObject guard = CreateGuard(brain, patrol, torchHandlerId);
            Undo.RegisterCreatedObjectUndo(guard, "Create PatrolGuard");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = guard;

            Debug.Log(
                "[NpcAi] Patrol Search example ready. Enter Play: the guard patrols, searches (torch +45°), and raises Spotted if the player is seen.",
                guard);
        }

        static NpcStateGraph LoadOrCreatePatrol()
        {
            NpcStateGraph graph = AssetDatabase.LoadAssetAtPath<NpcStateGraph>(PatrolPath);
            if (graph != null)
                return graph;
            graph = ScriptableObject.CreateInstance<NpcStateGraph>();

            NpcStateNodeData start = CharNode(NpcStateNodeKind.Start, new Vector2(80f, 180f));
            NpcStateNodeData playRun = CharNode(
                NpcStateNodeKind.CharacterAction,
                new Vector2(280f, 180f),
                NpcStateCharacterAction.PlayRun,
                waitUntilDone: false);
            NpcStateNodeData run1 = CharNode(
                NpcStateNodeKind.CharacterAction,
                new Vector2(500f, 180f),
                NpcStateCharacterAction.RunRandomLeftRight,
                distance: 2f);
            NpcStateNodeData run2 = CharNode(
                NpcStateNodeKind.CharacterAction,
                new Vector2(740f, 180f),
                NpcStateCharacterAction.RunRandomLeftRight,
                distance: 2f);
            NpcStateNodeData branch = CharNode(NpcStateNodeKind.RandomBranch, new Vector2(980f, 180f));
            NpcStateNodeData run3 = CharNode(
                NpcStateNodeKind.CharacterAction,
                new Vector2(1220f, 80f),
                NpcStateCharacterAction.RunRandomLeftRight,
                distance: 2f);
            NpcStateNodeData idle = CharNode(
                NpcStateNodeKind.CharacterAction,
                new Vector2(1220f, 280f),
                NpcStateCharacterAction.PlayIdle,
                waitUntilDone: false);
            NpcStateNodeData end = CharNode(NpcStateNodeKind.End, new Vector2(1460f, 180f));
            end.endOutcome = NpcStateOutcome.Completed;

            graph.nodes.AddRange(new[] { start, playRun, run1, run2, branch, run3, idle, end });
            AddEdge(graph, start.id, 0, playRun.id);
            AddEdge(graph, playRun.id, 0, run1.id);
            AddEdge(graph, run1.id, 0, run2.id);
            AddEdge(graph, run2.id, 0, branch.id);
            AddEdge(graph, branch.id, 0, run3.id);
            AddEdge(graph, branch.id, 1, idle.id);
            AddEdge(graph, run3.id, 0, idle.id);
            AddEdge(graph, idle.id, 0, end.id);

            AssetDatabase.CreateAsset(graph, PatrolPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        static NpcStateGraph LoadOrCreateSearch(out string torchHandlerId)
        {
            NpcStateGraph graph = AssetDatabase.LoadAssetAtPath<NpcStateGraph>(SearchPath);
            if (graph != null)
            {
                torchHandlerId = FindSceneActionHandlerId(graph);
                if (string.IsNullOrEmpty(torchHandlerId))
                {
                    torchHandlerId = System.Guid.NewGuid().ToString("N");
                    NpcStateNodeData scene = FindNode(graph, NpcStateNodeKind.SceneAction);
                    if (scene != null)
                    {
                        scene.executeHandlerId = torchHandlerId;
                        EditorUtility.SetDirty(graph);
                        AssetDatabase.SaveAssets();
                    }
                }

                return graph;
            }

            torchHandlerId = System.Guid.NewGuid().ToString("N");
            graph = ScriptableObject.CreateInstance<NpcStateGraph>();

            NpcStateNodeData start = CharNode(NpcStateNodeKind.Start, new Vector2(80f, 180f));
            NpcStateNodeData rotate = CharNode(NpcStateNodeKind.SceneAction, new Vector2(320f, 180f));
            rotate.executeHandlerId = torchHandlerId;
            NpcStateNodeData wait = CharNode(NpcStateNodeKind.Wait, new Vector2(560f, 180f));
            wait.duration = 0.25f;
            NpcStateNodeData end = CharNode(NpcStateNodeKind.End, new Vector2(800f, 180f));
            end.endOutcome = NpcStateOutcome.Completed;

            graph.nodes.AddRange(new[] { start, rotate, wait, end });
            AddEdge(graph, start.id, 0, rotate.id);
            AddEdge(graph, rotate.id, 0, wait.id);
            AddEdge(graph, wait.id, 0, end.id);

            AssetDatabase.CreateAsset(graph, SearchPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        static NpcStateMachineGraph LoadOrCreateBrain(NpcStateGraph patrol, NpcStateGraph search)
        {
            NpcStateMachineGraph graph = AssetDatabase.LoadAssetAtPath<NpcStateMachineGraph>(BrainPath);
            if (graph != null)
            {
                EnsureStateNodeName(graph, patrol, "Patrol");
                EnsureStateNodeName(graph, search, "Search");
                return graph;
            }

            graph = ScriptableObject.CreateInstance<NpcStateMachineGraph>();

            NpcStateMachineNodeData start = MachineNode(NpcStateMachineNodeKind.Start, new Vector2(80f, 200f));
            NpcStateMachineNodeData patrolNode = MachineNode(NpcStateMachineNodeKind.State, new Vector2(340f, 80f));
            patrolNode.stateGraph = patrol;
            patrolNode.nodeName = "Patrol";
            NpcStateMachineNodeData searchNode = MachineNode(NpcStateMachineNodeKind.State, new Vector2(340f, 320f));
            searchNode.stateGraph = search;
            searchNode.nodeName = "Search";
            NpcStateMachineNodeData spotted = MachineNode(NpcStateMachineNodeKind.Action, new Vector2(640f, 200f));
            spotted.actionId = "Spotted";
            NpcStateMachineNodeData end = MachineNode(NpcStateMachineNodeKind.End, new Vector2(900f, 200f));

            graph.nodes.AddRange(new[] { start, patrolNode, searchNode, spotted, end });
            AddMachineEdge(graph, start.id, 0, patrolNode.id);
            AddMachineEdge(graph, patrolNode.id, 0, searchNode.id);
            AddMachineEdge(graph, searchNode.id, 0, patrolNode.id);
            AddMachineEdge(graph, patrolNode.id, 2, spotted.id);
            AddMachineEdge(graph, searchNode.id, 2, spotted.id);
            AddMachineEdge(graph, spotted.id, 0, end.id);

            AssetDatabase.CreateAsset(graph, BrainPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        static GameObject CreateGuard(
            NpcStateMachineGraph brain,
            NpcStateGraph patrol,
            string torchHandlerId)
        {
            Vector3 spawn = FindPlayerPosition() + new Vector3(4f, 0f, 0f);

            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guard.name = GuardName;
            guard.transform.position = spawn;
            guard.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Object.DestroyImmediate(guard.GetComponent<CapsuleCollider>());
            MeshRenderer renderer = guard.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material material = new Material(renderer.sharedMaterial)
                {
                    color = new Color(0.28f, 0.22f, 0.16f)
                };
                renderer.sharedMaterial = material;
            }

            CharacterController controller = guard.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;

            NpcLocomotionController locomotion = guard.AddComponent<NpcLocomotionController>();
            locomotion.SetLocomotionEnabled(false);

            SerumActionBridge bridge = guard.AddComponent<SerumActionBridge>();
            NpcStateActor actor = guard.AddComponent<NpcStateActor>();
            NpcStateMachine machine = guard.AddComponent<NpcStateMachine>();
            NpcStateMachineBridge interruptBridge = guard.AddComponent<NpcStateMachineBridge>();

            SerializedObject actorSo = new SerializedObject(actor);
            actorSo.FindProperty("playOnEnable").boolValue = false;
            actorSo.FindProperty("actionBridge").objectReferenceValue = bridge;
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject machineSo = new SerializedObject(machine);
            machineSo.FindProperty("graph").objectReferenceValue = brain;
            machineSo.FindProperty("playOnEnable").boolValue = true;
            machineSo.FindProperty("actor").objectReferenceValue = actor;
            machineSo.ApplyModifiedPropertiesWithoutUndo();

            machine.EnsureAction("Spotted");

            string patrolNodeId = FindStateNodeId(brain, patrol);
            BindInterruptBridge(interruptBridge, machine, patrolNodeId);

            GameObject torch = EnsureTorchSpotAndCone(guard, machine, torchHandlerId, patrolNodeId);

            EditorUtility.SetDirty(guard);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(torch.GetComponent<NpcSceneActionHandler>());
            return guard;
        }

        static void UpgradeExistingGuard(
            GameObject guard,
            NpcStateMachineGraph brain,
            NpcStateGraph patrol,
            string torchHandlerId)
        {
            SideScrollerController playerMotor = guard.GetComponent<SideScrollerController>();
            if (playerMotor != null)
                Undo.DestroyObjectImmediate(playerMotor);

            if (guard.GetComponent<NpcLocomotionController>() == null)
            {
                NpcLocomotionController npcMotor = Undo.AddComponent<NpcLocomotionController>(guard);
                npcMotor.SetLocomotionEnabled(false);
            }

            SerumActionBridge bridge = guard.GetComponent<SerumActionBridge>();
            if (bridge != null)
            {
                SerializedObject bridgeSo = new SerializedObject(bridge);
                SerializedProperty locomotionProp = bridgeSo.FindProperty("locomotion");
                if (locomotionProp != null)
                    locomotionProp.objectReferenceValue = null;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            NpcStateMachine machine = guard.GetComponent<NpcStateMachine>();
            string patrolNodeId = FindStateNodeId(brain != null ? brain : machine != null ? machine.Graph : null, patrol);
            EnsureInterruptBridge(guard, machine, patrolNodeId);
            EnsureTorchSpotAndCone(guard, machine, torchHandlerId, patrolNodeId);
            EditorUtility.SetDirty(guard);
        }

        static GameObject EnsureTorchSpotAndCone(
            GameObject guard,
            NpcStateMachine machine,
            string torchHandlerId,
            string patrolNodeId)
        {
            Transform torchTransform = FindChild(guard.transform, "Torch");
            GameObject torch = torchTransform != null ? torchTransform.gameObject : new GameObject("Torch");
            torch.name = "Torch";
            torch.transform.SetParent(guard.transform, false);
            if (torchTransform == null)
            {
                torch.transform.localPosition = new Vector3(0f, 0.55f, 0.35f);
                torch.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            }

            Light light = torch.GetComponent<Light>();
            bool createdLight = light == null;
            if (createdLight)
                light = torch.AddComponent<Light>();

            light.type = LightType.Spot;
            if (createdLight)
            {
                light.range = 10f;
                light.spotAngle = 50f;
                light.intensity = 5f;
                light.color = new Color(1f, 0.86f, 0.55f);
            }
            else
            {
                if (light.range <= 0.01f)
                    light.range = 10f;
                if (light.spotAngle <= 1f)
                    light.spotAngle = 50f;
                if (light.intensity <= 0.01f)
                    light.intensity = 5f;
                if (light.color.maxColorComponent <= 0.01f)
                    light.color = new Color(1f, 0.86f, 0.55f);
            }

            light.innerSpotAngle = Mathf.Clamp(light.spotAngle * 0.7f, 1f, light.spotAngle);

            if (torch.GetComponent<SerumActionBridge>() == null)
                torch.AddComponent<SerumActionBridge>();

            NpcTorchSweep sweep = torch.GetComponent<NpcTorchSweep>();
            if (sweep == null)
                sweep = torch.AddComponent<NpcTorchSweep>();

            SerializedObject sweepSo = new SerializedObject(sweep);
            sweepSo.FindProperty("bridge").objectReferenceValue = torch.GetComponent<SerumActionBridge>();
            if (sweepSo.FindProperty("yawDegrees").floatValue == 0f)
                sweepSo.FindProperty("yawDegrees").floatValue = 45f;
            sweepSo.ApplyModifiedPropertiesWithoutUndo();

            NpcSceneActionHandler handler = torch.GetComponent<NpcSceneActionHandler>();
            if (handler == null)
                handler = torch.AddComponent<NpcSceneActionHandler>();

            if (!string.IsNullOrEmpty(torchHandlerId))
                handler.AssignHandlerId(torchHandlerId);

            if (handler.OnExecute.GetPersistentEventCount() == 0)
                UnityEventTools.AddPersistentListener(handler.OnExecute, sweep.Sweep);

            Transform triggerTransform = FindChild(guard.transform, "SpotTrigger");
            GameObject triggerGo = triggerTransform != null ? triggerTransform.gameObject : new GameObject("SpotTrigger");
            triggerGo.name = "SpotTrigger";
            triggerGo.transform.SetParent(torch.transform, false);
            triggerGo.transform.localPosition = Vector3.zero;
            triggerGo.transform.localRotation = Quaternion.identity;
            triggerGo.transform.localScale = Vector3.one;

            NpcSpotTrigger spot = triggerGo.GetComponent<NpcSpotTrigger>();
            if (spot == null)
                spot = triggerGo.AddComponent<NpcSpotTrigger>();

            SerializedObject spotSo = new SerializedObject(spot);
            spotSo.FindProperty("machine").objectReferenceValue = machine;
            spotSo.FindProperty("spotLight").objectReferenceValue = light;
            SerializedProperty interruptProp = spotSo.FindProperty("interruptNodeId");
            if (interruptProp != null)
                interruptProp.stringValue = patrolNodeId ?? "";
            spotSo.ApplyModifiedPropertiesWithoutUndo();
            spot.Bind(machine, light);
            return torch;
        }

        static void EnsureInterruptBridge(GameObject guard, NpcStateMachine machine, string patrolNodeId)
        {
            NpcStateMachineBridge interruptBridge = guard.GetComponent<NpcStateMachineBridge>();
            if (interruptBridge == null)
                interruptBridge = Undo.AddComponent<NpcStateMachineBridge>(guard);

            BindInterruptBridge(interruptBridge, machine, patrolNodeId);
        }

        static void BindInterruptBridge(NpcStateMachineBridge interruptBridge, NpcStateMachine machine, string patrolNodeId)
        {
            if (interruptBridge == null)
                return;

            SerializedObject interruptSo = new SerializedObject(interruptBridge);
            interruptSo.FindProperty("machine").objectReferenceValue = machine;
            interruptSo.FindProperty("interruptNodeId").stringValue = patrolNodeId ?? "";
            interruptSo.ApplyModifiedPropertiesWithoutUndo();
            interruptBridge.Bind(machine, patrolNodeId);
        }

        static string FindStateNodeId(NpcStateMachineGraph brain, NpcStateGraph state)
        {
            if (brain == null || brain.nodes == null || state == null)
                return "";

            for (int i = 0; i < brain.nodes.Count; i++)
            {
                NpcStateMachineNodeData node = brain.nodes[i];
                if (node != null && node.kind == NpcStateMachineNodeKind.State && node.stateGraph == state)
                    return node.id;
            }

            return "";
        }

        static void EnsureStateNodeName(NpcStateMachineGraph graph, NpcStateGraph state, string nodeName)
        {
            if (graph == null || graph.nodes == null || state == null || string.IsNullOrEmpty(nodeName))
                return;

            bool dirty = false;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                NpcStateMachineNodeData node = graph.nodes[i];
                if (node == null || node.kind != NpcStateMachineNodeKind.State || node.stateGraph != state)
                    continue;

                if (node.nodeName == nodeName)
                    continue;

                node.nodeName = nodeName;
                dirty = true;
            }

            if (!dirty)
                return;

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;

            Transform direct = root.Find(name);
            if (direct != null)
                return direct;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == name)
                    return children[i];
            }

            return null;
        }

        static Vector3 FindPlayerPosition()
        {
            SideScrollerController player = Object.FindFirstObjectByType<SideScrollerController>();
            return player != null ? player.transform.position : Vector3.zero;
        }

        static NpcStateNodeData CharNode(
            NpcStateNodeKind kind,
            Vector2 position,
            NpcStateCharacterAction action = NpcStateCharacterAction.PlayIdle,
            float distance = 4f,
            bool waitUntilDone = true)
        {
            return new NpcStateNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position,
                characterAction = action,
                distance = distance,
                speed = 4f,
                waitUntilDone = waitUntilDone
            };
        }

        static NpcStateMachineNodeData MachineNode(NpcStateMachineNodeKind kind, Vector2 position)
        {
            return new NpcStateMachineNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position
            };
        }

        static void AddEdge(NpcStateGraph graph, string fromId, int fromPort, string toId)
        {
            graph.edges.Add(new NpcStateEdgeData { fromId = fromId, fromPort = fromPort, toId = toId });
        }

        static void AddMachineEdge(NpcStateMachineGraph graph, string fromId, int fromPort, string toId)
        {
            graph.edges.Add(new NpcStateMachineEdgeData { fromId = fromId, fromPort = fromPort, toId = toId });
        }

        static NpcStateNodeData FindNode(NpcStateGraph graph, NpcStateNodeKind kind)
        {
            if (graph == null || graph.nodes == null)
                return null;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i] != null && graph.nodes[i].kind == kind)
                    return graph.nodes[i];
            }

            return null;
        }

        static string FindSceneActionHandlerId(NpcStateGraph graph)
        {
            NpcStateNodeData node = FindNode(graph, NpcStateNodeKind.SceneAction);
            return node != null ? node.executeHandlerId : null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
