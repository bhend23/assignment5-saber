using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(AudioSource))]
public class LightsaberBlade : MonoBehaviour
{
    // Sounds for turning the blade on and off (assign in the Inspector)
    public AudioClip activateClip;
    public AudioClip deactivateClip;
    private AudioSource audioSource;

    // 1. Extend/collapse speed. Despite the name, this acts as the time in seconds
    //    a full extend/collapse takes (extendDelta = scaleMax / extendSpeed).
    [SerializeField] private float extendSpeed = 0.1f;

    // 2. Lightsaber state (on/off)
    private bool weaponActive;

    // 3. Minimum scaling value
    private const float SCALE_MIN = 0f;

    // 4. Maximum scaling value (set from the cylinder's initial local y scale)
    private float scaleMax;

    // 5. Interpolation value: how much to scale per second
    private float extendDelta;

    // 6. Current scale of the blade along its local y axis
    private float scaleCurrent;

    // 7. Initial local x and z scale values
    private float localScaleX;
    private float localScaleZ;

    public GameObject blade;

    // Which end of the blade touches the hilt. Untick if the blade
    // collapses toward its tip instead of into the hilt.
    [SerializeField] private bool hiltAtBottom = true;

    // Hilt end of the blade (in the parent's space), kept fixed while scaling
    private Vector3 bladeBaseLocal;
    // Direction from the hilt toward the tip (in the parent's space)
    private Vector3 bladeDirLocal;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Fall back to this object if no blade was assigned in the Inspector
        if (blade == null)
            blade = gameObject;

        if (blade == gameObject)
            Debug.LogWarning("LightsaberBlade is on the blade itself. Deactivating the blade will also stop this script, " +
                             "so it can't turn back on. Put this script on the hilt and drag the blade into the 'blade' slot.");

        // Save the initial local x and z scales
        localScaleX = blade.transform.localScale.x;
        localScaleZ = blade.transform.localScale.z;

        // Max scale = starting local y scale
        scaleMax = blade.transform.localScale.y;

        // A Unity cylinder is 2 units tall, so its half-length equals its y scale.
        // Find the hilt end so we can keep it fixed while the blade scales.
        bladeDirLocal = blade.transform.localRotation * (hiltAtBottom ? Vector3.up : Vector3.down);
        bladeBaseLocal = blade.transform.localPosition - bladeDirLocal * scaleMax;

        // Blade starts on and fully extended
        scaleCurrent = scaleMax;

        // Amount to scale per second
        extendDelta = scaleMax / extendSpeed;

        weaponActive = true;
    }

    void Update()
    {
        if (TogglePressed())
        {
            // On -> collapse (negative delta), off -> extend (positive delta)
            extendDelta = weaponActive ? -Mathf.Abs(extendDelta)
                                       :  Mathf.Abs(extendDelta);

            // On -> play deactivate sound, off -> play activate sound
            PlaySound(weaponActive ? deactivateClip : activateClip);
        }

        // Apply the interpolation value, scaled by frame time
        scaleCurrent += extendDelta * Time.deltaTime;

        // Keep the scale between min and max
        scaleCurrent = Mathf.Clamp(scaleCurrent, SCALE_MIN, scaleMax);

        // Scale only along the blade's local y axis
        blade.transform.localScale = new Vector3(localScaleX, scaleCurrent, localScaleZ);

        // Move the center so the hilt end stays put (grows/shrinks from the hilt, not the middle)
        blade.transform.localPosition = bladeBaseLocal + bladeDirLocal * scaleCurrent;

        // Blade counts as on if it has any length
        weaponActive = scaleCurrent > 0;

        // Show the blade when on, hide it when off
        if (weaponActive && !blade.activeSelf)
        {
            blade.SetActive(true);
        }
        else if (!weaponActive && blade.activeSelf)
        {
            blade.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return; // No sound assigned yet

        // Swap the clip and play it, cutting off the previous sound if it's still going
        audioSource.clip = clip;
        audioSource.Play();
    }

    private bool TogglePressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        // Project is set to the new Input System only, where Input.GetKeyDown throws.
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
    }
}
