﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorNode
{
    public BaseCutsceneNode actualNode;

    public float width;
    public float height;

    public bool isDragged;
    public bool isSelected;

    public Action<EditorNode> OnRemoveNode;
    public Action<EditorNode> OnDrawNode;

    public Rect rect;

    public List<EditorConnectionPoint> inPoints;
    public List<EditorConnectionPoint> outPoints;

    private NodeBasedEditor editor;

    private GUIStyle headerStyle;
    private GUIStyle boxStyle;

    private GUIStyle outPointStyle;
    private Action<EditorConnectionPoint> OnClickOutPoint;

    public EditorNode(NodeBasedEditor editor, BaseCutsceneNode actualNode, Vector2 position, GUIStyle headerStyle, GUIStyle boxStyle) {
        rect = new Rect(position.x, position.y, 140, 20);
        this.editor = editor;
        this.actualNode = actualNode;
        this.headerStyle = headerStyle;
        this.boxStyle = boxStyle;
    }

    public void prepareConnections(GUIStyle inPointStyle, GUIStyle outPointStyle, Action<EditorConnectionPoint> OnClickInPoint, Action<EditorConnectionPoint> OnClickOutPoint) {
        inPoints = new List<EditorConnectionPoint>();
        inPoints.Add(new EditorConnectionPoint(this, ConnectionPointType.In, inPointStyle, OnClickInPoint, 0, ""));

        this.outPointStyle = outPointStyle;
        this.OnClickOutPoint = OnClickOutPoint;

        outPoints = new List<EditorConnectionPoint>();
        for (int i = 1; i <= actualNode.outputNodeLabels.Count; i++) {
            outPoints.Add(new EditorConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, i, actualNode.outputNodeLabels[i - 1]));
        }

        rect = new Rect(rect.x, rect.y, rect.width, getHeight());
    }

    public void reloadOutConnections(GUIStyle outPointStyle, Action<EditorConnectionPoint> OnClickOutPoint) {
        outPoints = new List<EditorConnectionPoint>();
        for (int i = 1; i <= actualNode.outputNodeLabels.Count; i++) {
            outPoints.Add(new EditorConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, i, actualNode.outputNodeLabels[i - 1]));
        }

        rect = new Rect(rect.x, rect.y, rect.width, getHeight());
    }

    public float getHeight(){
        return 10 + 25 * actualNode.outputNodeLabels.Count;
    }

    public void Drag(Vector2 delta) {
        rect.position += delta;
    }

    public void Draw() {
        //Check if the actual node still exists, and if yes, draw the node
        if(actualNode == null){
            OnClickRemoveNode();
        } else {
            //Draw the BG box
            GUI.Box(rect, "", boxStyle);

            //Draw the header
            GUI.Box(new Rect(rect.x, rect.y - 20, rect.width, 20), "", headerStyle);

            if (Selection.activeGameObject == actualNode.gameObject) {
                GUI.Label(new Rect(rect.x + 5, rect.y - 15, rect.width - 10, 20), actualNode.name, EditorStyles.boldLabel);
            } else {
                GUI.Label(new Rect(rect.x + 5, rect.y - 15, rect.width - 10, 20), actualNode.name, EditorStyles.wordWrappedLabel);
            }

            //Remove button
            if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y - 20, 20, 20), "X"))
                OnClickRemoveNode();

            //Draw Inputs and Outputs
            foreach (EditorConnectionPoint connectionPoint in inPoints) {
                connectionPoint.Draw();
            }

            if (outPoints.Count > 0) {
                foreach (EditorConnectionPoint connectionPoint in outPoints) {
                    connectionPoint.Draw();
                }
            }

            if (OnDrawNode != null) {
                OnDrawNode(this);
            }

            if (actualNode.outputNodeLabels.Count != outPoints.Count) {
                reloadOutConnections(outPointStyle, OnClickOutPoint);
            }
        }
    }

    public virtual bool ProcessEvents(Event e) {
        switch (e.type) {
            case EventType.MouseDown:
                if (e.button == 0) {
                    if (new Rect(rect.x, rect.y - 20, rect.width, rect.height + 20).Contains(e.mousePosition)) {
                        Selection.SetActiveObjectWithContext(actualNode, null);
                        isDragged = true;
                        GUI.changed = true;
                        isSelected = true;
                    } else {
                        GUI.changed = true;
                        isSelected = false;
                    }
                }

                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition)) {
                    ProcessContextMenu();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                isDragged = false;
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isDragged) {
                    Drag(e.delta);
                    e.Use();
                    MoveActualNode();
                    return true;
                }
                break;
        }

        return false;
    }

    private void ProcessContextMenu() {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }

    private void OnClickRemoveNode() {
        if (OnRemoveNode != null) {
            OnRemoveNode(this);
        }
    }

    private void MoveActualNode() {
        float offsetScaleFactor = NodeBasedEditor.offsetScaleFactor;
        Vector2 offset = editor.GetOffset();

        switch (editor.projectionPlane) {
            case NodeBasedEditor.ProjectionPlaneType.XY:
                actualNode.transform.localPosition = new Vector3((rect.position.x - offset.x) / offsetScaleFactor , (rect.position.y - offset.y) / offsetScaleFactor, actualNode.transform.localPosition.z);
                break;
            case NodeBasedEditor.ProjectionPlaneType.XZ:
                actualNode.transform.localPosition = new Vector3((rect.position.x - offset.x) / offsetScaleFactor, actualNode.transform.localPosition.y, (rect.position.y - offset.y) / offsetScaleFactor);
                break;
            case NodeBasedEditor.ProjectionPlaneType.YZ:
                actualNode.transform.localPosition = new Vector3(actualNode.transform.localPosition.x, (rect.position.y - offset.y) / offsetScaleFactor, (rect.position.x - offset.x) / offsetScaleFactor);
                break;
            default:
                actualNode.transform.localPosition = new Vector3((rect.position.x - offset.x) / offsetScaleFactor, (rect.position.y - offset.y) / offsetScaleFactor, actualNode.transform.localPosition.z);
                break;
        }
    }

    public Rect GetRectForLine(int row, float lineHeight = 20, float padding = 0) {
        return new Rect(rect.x + padding, rect.y + row * (lineHeight + padding), rect.width - padding, lineHeight);
    }

    public EditorNode GetNextNode(int connectionId) {
        return outPoints[connectionId].connection.inPoint.node;
    }

    public EditorNode GetPreviousNode(int connectionId) {
        return inPoints[connectionId].connection.outPoint.node;
    }
}