using UnityEngine;
using UnityEngine.XR;

public class RuntimeModeSwitcher : MonoBehaviour
{
    public GameObject xrRig, desktopRig;

    void Start()
    {
        bool isXR = XRSettings.isDeviceActive;
        xrRig.SetActive(isXR);
        desktopRig.SetActive(!isXR);
    }
}
