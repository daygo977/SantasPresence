using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DoorScript
{
    [RequireComponent(typeof(AudioSource))]
    public class Door : MonoBehaviour
    {
        public bool open;
        public float smooth = 1.0f;

        float DoorOpenAngle = -90.0f;
        float DoorCloseAngle = 0.0f;

        [Header("Audio")]
        public AudioSource asource;
        public AudioClip openDoor;
        public AudioClip closeDoor;

        [Range(0f, 1f)]
        public float doorVolume = 0.7f;

        [Header("3D Audio Settings")]
        public float minDistance = 1.5f;
        public float maxDistance = 10f;

        void Start()
        {
            asource = GetComponent<AudioSource>();

            // Force correct 3D spatial setup
            asource.spatialBlend = 1f; // FULL 3D
            asource.rolloffMode = AudioRolloffMode.Logarithmic;
            asource.minDistance = minDistance;
            asource.maxDistance = maxDistance;
            asource.playOnAwake = false;
        }

        void Update()
        {
            if (open)
            {
                var target = Quaternion.Euler(0, DoorOpenAngle, 0);
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    target,
                    Time.deltaTime * 5 * smooth
                );
            }
            else
            {
                var target1 = Quaternion.Euler(0, DoorCloseAngle, 0);
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    target1,
                    Time.deltaTime * 5 * smooth
                );
            }
        }

        public void OpenDoor()
        {
            open = !open;

            asource.clip = open ? openDoor : closeDoor;
            asource.volume = doorVolume;
            asource.Play();
        }
    }
}
