using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BossEnemyPatrol))]
public class BossDebugVisualizer : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnDrawGizmos()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isActiveAndEnabled) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, agent.destination);
        Gizmos.DrawSphere(agent.destination, 0.2f);

        if (agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i] + Vector3.up * 0.05f, corners[i + 1] + Vector3.up * 0.05f);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BossDebugVisualizer))]
public class BossDebugVisualizerEditor : Editor
{
    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BossDebugVisualizer vis = (BossDebugVisualizer)target;
        BossEnemyPatrol boss = vis.GetComponent<BossEnemyPatrol>();
        NavMeshAgent agent = vis.GetComponent<NavMeshAgent>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see live state.", MessageType.Info);
            return;
        }

        if (boss != null)
        {
            EditorGUILayout.LabelField("State", boss.CurrentState.ToString());
            EditorGUILayout.LabelField("Room", boss.CurrentRoomName);
        }

        if (agent != null)
        {
            EditorGUILayout.LabelField("Destination", agent.destination.ToString("F1"));
            EditorGUILayout.LabelField("Remaining Dist", agent.remainingDistance.ToString("F2") + " m");
        }
    }
}
#endif
