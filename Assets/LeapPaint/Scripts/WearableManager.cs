﻿using UnityEngine;
using System.Collections.Generic;
using Leap.Unity;

public class WearableManager : MonoBehaviour {

  public Transform _centerEyeAnchor;

  [Header("Hand State Tracking")]
  public IHandModel _leftHand;
  public PalmDirectionDetector _leftPalmFacingDetector;
  public PinchDetector _leftPinchDetector;
  public IHandModel _rightHand;
  public PalmDirectionDetector _rightPalmFacingDetector;
  public PinchDetector _rightPinchDetector;

  [Header("Pinch Grabbable Wearables")]
  public float _pinchGrabDistance = 0.05F;

  // Wearable/Anchor registration
  public WearableUI[] _wearableUIs;
  public WearableAnchor[] _wearableAnchors;
  public List<IWearable> _wearables = new List<IWearable>();

  [Header("Effects")]
  public Material _fadeableAnchorRingMaterial;
  public Material _opaqueAnchorRingMaterial;

  // Hand state tracking
  private bool _isLeftHandTracked;
  private bool _isRightHandTracked;
  private bool _isLeftPalmFacingCamera;
  private bool _isRightPalmFacingCamera;
  private Chirality _lastHandFacingCamera;
  
  // Wearable state tracking
  private IWearable _leftGrabbedWearable = null;
  private IWearable _rightGrabbedWearable = null;

  protected void Start() {
    _leftPinchDetector.OnActivate.AddListener(OnLeftPinchDetected);
    _leftPinchDetector.OnDeactivate.AddListener(OnLeftPinchEnded);
    _rightPinchDetector.OnActivate.AddListener(OnRightPinchDetected);
    _rightPinchDetector.OnDeactivate.AddListener(OnRightPinchEnded);

    for (int i = 0; i < _wearableUIs.Length; i++) {
      _wearables.Add(_wearableUIs[i]);
    }
    for (int i = 0; i < _wearableAnchors.Length; i++) {
      _wearables.Add(_wearableAnchors[i]);
    }
  }

  protected void Update() {
    if (_leftHand.IsTracked && !_isLeftHandTracked) {
      OnLeftHandBeganTracking();
      _isLeftHandTracked = true;
    }
    else if (!_leftHand.IsTracked && _isLeftHandTracked) {
      OnLeftHandStoppedTracking();
      _isLeftHandTracked = false;
    }

    if (_rightHand.IsTracked && !_isRightHandTracked) {
      OnRightHandBeganTracking();
      _isRightHandTracked = true;
    }
    else if (!_rightHand.IsTracked && _isRightHandTracked) {
      OnRightHandStoppedTracking();
      _isRightHandTracked = false;
    }

    if (_leftPalmFacingDetector.IsActive && !_isLeftPalmFacingCamera) {
      OnLeftHandBeganFacingCamera();
      _isLeftPalmFacingCamera = true;
    }
    else if (!_leftPalmFacingDetector.IsActive && _isLeftPalmFacingCamera) {
      OnLeftHandStoppedFacingCamera();
      _isLeftPalmFacingCamera = false;
    }

    if (_rightPalmFacingDetector.IsActive && !_isRightPalmFacingCamera) {
      OnRightHandBeganFacingCamera();
      _isRightPalmFacingCamera = true;
    }
    else if (!_rightPalmFacingDetector.IsActive && _isRightPalmFacingCamera) {
      OnRightHandStoppedFacingCamera();
      _isRightPalmFacingCamera = false;
    }
  }

  private void OnLeftHandBeganTracking() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyHandTracked(true, Chirality.Left);
    }
  }
  private void OnLeftHandStoppedTracking() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyHandTracked(false, Chirality.Left);
    }
  }
  private void OnLeftHandBeganFacingCamera() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPalmFacingCamera(true, Chirality.Left);
    }
  }
  private void OnLeftHandStoppedFacingCamera() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPalmFacingCamera(false, Chirality.Left);
    }
  }

  private void OnRightHandBeganTracking() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyHandTracked(true, Chirality.Right);
    }
  }
  private void OnRightHandStoppedTracking() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyHandTracked(false, Chirality.Right);
    }
  }
  private void OnRightHandBeganFacingCamera() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPalmFacingCamera(true, Chirality.Right);
    }
  }
  private void OnRightHandStoppedFacingCamera() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPalmFacingCamera(false, Chirality.Right);
    }
  }

  private void OnLeftPinchDetected() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPinchChanged(true, Chirality.Left);
    }
    TryGrab(EvaluatePossiblePinch(_leftPinchDetector), Chirality.Left);
  }

  private void OnLeftPinchEnded() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPinchChanged(false, Chirality.Left);
    }
    if (_leftGrabbedWearable != null) {
      _leftGrabbedWearable.ReleaseFromGrab(_leftPinchDetector.transform);
    }
  }

  private void OnRightPinchDetected() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPinchChanged(true, Chirality.Right);
    }
    TryGrab(EvaluatePossiblePinch(_rightPinchDetector), Chirality.Right);
  }

  private void OnRightPinchEnded() {
    for (int i = 0; i < _wearables.Count; i++) {
      _wearables[i].NotifyPinchChanged(false, Chirality.Right);
    }
    if (_rightGrabbedWearable != null) {
      _rightGrabbedWearable.ReleaseFromGrab(_rightPinchDetector.transform);
    }
  }

  /// <summary> Returns the closest WearableUI to the PinchDetector, or null of they are all further than _pinchGrabDistance.</summary>
  private IWearable EvaluatePossiblePinch(PinchDetector pinchToTest) {
    IWearable closestWearable = null;
    float closestDistance = 1000000F;
    float pinchWearableDistance = 0F;
    for (int i = 0; i < _wearables.Count; i++) {
      if (_wearables[i].CanBeGrabbed()) {
        pinchWearableDistance = Vector3.Distance(_wearables[i].GetPosition(), pinchToTest.transform.position);
        if (pinchWearableDistance < _pinchGrabDistance && pinchWearableDistance < closestDistance) {
          closestDistance = pinchWearableDistance;
          closestWearable = _wearables[i];
        }
      }
    }
    return closestWearable;
  }

  private void TryGrab(IWearable toGrab, Chirality whichHand) {
    if (toGrab == null) return;
    if (toGrab.BeGrabbedBy((whichHand == Chirality.Left ? _leftPinchDetector.transform : _rightPinchDetector.transform))) {
      if (whichHand == Chirality.Left) {
        _leftGrabbedWearable = toGrab;
        if (_rightGrabbedWearable == toGrab) {
          _rightGrabbedWearable = null;
        }
      }
      else {
        _rightGrabbedWearable = toGrab;
        if (_leftGrabbedWearable == toGrab) {
          _leftGrabbedWearable = null;
        }
      }
    }
  }

  public IWearable LastGrabbedByLeftHand() {
    return _leftGrabbedWearable;
  }

  public IWearable LastGrabbedByRightHand() {
    return _rightGrabbedWearable;
  }

  public Transform GetCenterEyeAnchor() {
    return _centerEyeAnchor;
  }

}