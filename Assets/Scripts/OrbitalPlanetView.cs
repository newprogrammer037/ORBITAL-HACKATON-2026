using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class OrbitalPlanetView : MonoBehaviour
{
    private Camera planetCamera;
    private Transform planetRoot;
    private Transform[] markers;
    private Vector3[] markerDirections;
    private Action<int> selectionCallback;
    private float cameraDistance = 5f;
    private Vector2 lastPointer;
    private bool pointerDownOnPlanet;
    private bool dragged;

    public void Initialize(Camera camera, Transform root, Transform[] regionMarkers, Action<int> onSelected)
    {
        planetCamera = camera;
        planetRoot = root;
        markers = regionMarkers;
        selectionCallback = onSelected;
        markerDirections = new Vector3[markers.Length];
        for (int i = 0; i < markers.Length; i++) markerDirections[i] = markers[i].localPosition.normalized;
        UpdateCameraDistance();
    }

    public void SetSelected(int index)
    {
        if (markers == null) return;
        for (int i = 0; i < markers.Length; i++)
        {
            float scale = i == index ? .19f : .12f;
            markers[i].localScale = Vector3.one * scale;
            var renderer = markers[i].GetComponent<Renderer>();
            var material = renderer.material;
            Color color = i == index ? new Color(1f, .67f, .19f) : new Color(.14f, .88f, 1f);
            material.color = color;
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * (i == index ? 2.2f : 1.2f));
        }
    }

    private void Update()
    {
        if (!planetCamera || !planetCamera.enabled || Mouse.current == null) return;
        Vector2 pointer = Mouse.current.position.ReadValue();
        var rect = (RectTransform)transform;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerDownOnPlanet = RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, null);
            dragged = false;
            lastPointer = pointer;
        }

        if (pointerDownOnPlanet && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = pointer - lastPointer;
            if (delta.sqrMagnitude > 4f) dragged = true;
            if (dragged)
            {
                planetRoot.Rotate(Vector3.up, -delta.x * .32f, Space.World);
                planetRoot.Rotate(Vector3.right, delta.y * .24f, Space.World);
            }
            lastPointer = pointer;
        }

        if (pointerDownOnPlanet && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (!dragged) TrySelectAtScreenPoint(pointer);
            pointerDownOnPlanet = false;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, null))
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > .01f)
            {
                cameraDistance = Mathf.Clamp(cameraDistance - scroll * .0025f, 3.6f, 7.5f);
                UpdateCameraDistance();
            }
        }
    }

    public bool TrySelectAtScreenPoint(Vector2 screenPosition)
    {
        if (planetCamera == null || planetRoot == null || markers == null) return false;
        var rect = (RectTransform)transform;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null)) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out Vector2 local)) return false;
        Vector2 normalized = Rect.PointToNormalized(rect.rect, local);
        if (SystemInfo.graphicsUVStartsAtTop) normalized.y = 1f - normalized.y;
        Ray ray = planetCamera.ViewportPointToRay(new Vector3(normalized.x, normalized.y, 0));
        Vector3 offset = ray.origin - planetRoot.position;
        const float radius = 1.25f;
        float projection = Vector3.Dot(offset, ray.direction);
        float discriminant = projection * projection - (offset.sqrMagnitude - radius * radius);
        if (discriminant < 0f) return false;
        float distance = -projection - Mathf.Sqrt(discriminant);
        if (distance < 0f) distance = -projection + Mathf.Sqrt(discriminant);
        if (distance < 0f) return false;
        Vector3 surfacePoint = ray.GetPoint(distance);
        Vector3 localDirection = planetRoot.InverseTransformDirection(surfacePoint - planetRoot.position).normalized;
        int nearest = -1;
        float bestAngle = float.MaxValue;
        for (int i = 0; i < markerDirections.Length; i++)
        {
            float angle = Vector3.Angle(localDirection, markerDirections[i]);
            if (angle < bestAngle) { bestAngle = angle; nearest = i; }
        }
        if (nearest < 0) return false;
        selectionCallback?.Invoke(nearest);
        return true;
    }

    private void UpdateCameraDistance()
    {
        if (planetCamera) planetCamera.transform.localPosition = new Vector3(0f, 0f, -cameraDistance);
    }
}
