using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{   
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private CinemachinePositionComposer positionComposer;
    
    [Header("Edge Scrolling Settings")]
    [SerializeField] private bool enableEdgeScrolling = true;
    [SerializeField] private float edgeScrollSize = 35f;

    [Header("Speed Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomAmount = 2f;

    [Header("Zoom Settings (Distance)")]
    [SerializeField] private float minZoomDistance = 2f; // Lowered so you can zoom in very close
    [SerializeField] private float maxZoomDistance = 20f;
    private float targetDistance;

    private void Start()
    {
        if (enableEdgeScrolling)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }

        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged += SnapToSelectedUnit_OnSelectedUnitChanged;
        }

        if (cinemachineCamera == null)
        {
            Debug.LogError("CinemachineCamera is not assigned!");
            enabled = false;
            return;
        }

        positionComposer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();

        if (positionComposer == null)
        {
            Debug.LogError("CinemachinePositionComposer not found! Please re-add it to your Cinemachine Camera.");
            enabled = false;
            return;
        }

        targetDistance = positionComposer.CameraDistance;
    }

    private void OnDestroy()
    {
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged -= SnapToSelectedUnit_OnSelectedUnitChanged;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
        }

        if (enableEdgeScrolling && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            bool isMouseInsideScreen = mousePos.x >= 0 && mousePos.x <= Screen.width && 
                                       mousePos.y >= 0 && mousePos.y <= Screen.height;

            if (isMouseInsideScreen)
            {
                if (mousePos.y >= Screen.height - edgeScrollSize) moveInput.y += 1f;
                if (mousePos.y <= edgeScrollSize) moveInput.y -= 1f;
                if (mousePos.x >= Screen.width - edgeScrollSize) moveInput.x += 1f;
                if (mousePos.x <= edgeScrollSize) moveInput.x -= 1f;
            }
        }

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        Vector3 cameraForward = cinemachineCamera.transform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.01f)
        {
            cameraForward = cinemachineCamera.transform.up;
            cameraForward.y = 0f;
        }
        
        Vector3 cameraRight = cinemachineCamera.transform.right;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveVector = (cameraForward * moveInput.y) + (cameraRight * moveInput.x);
        transform.position += moveVector * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        if (Keyboard.current == null) return;

        float rotationInput = 0f;

        if (Keyboard.current.qKey.isPressed) rotationInput += 1f;
        if (Keyboard.current.eKey.isPressed) rotationInput -= 1f;

        transform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null || positionComposer == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;

        // Reversed the + and - so scrolling up zooms in and scrolling down zooms out
        if (scrollY > 0)
            targetDistance -= zoomAmount;
        else if (scrollY < 0)
            targetDistance += zoomAmount;

        targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);

        positionComposer.CameraDistance = Mathf.Lerp(
            positionComposer.CameraDistance,
            targetDistance,
            Time.deltaTime * zoomSpeed
        );
    }

    private void SnapToSelectedUnit_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit != null)
        {
            transform.position = selectedUnit.transform.position;
        }
    }
}