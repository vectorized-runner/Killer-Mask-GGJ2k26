using UnityEngine;
using System.Collections;

namespace Scene
{
    public class AutoDoorScript : MonoBehaviour
    {
        [SerializeField] private Vector3 _closedRotation;
        [SerializeField] private Vector3 _openRotation;
        [SerializeField] private float _openDuration = 2f;
        [SerializeField] private float _openCloseSpeed = 2f;
        [SerializeField] private AudioClip _openSound;
        private AudioSource _audioSource;
        private Coroutine _doorCoroutine;
        private bool _isOpen;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !_isOpen)
            {
                if (_doorCoroutine != null)
                    StopCoroutine(_doorCoroutine);
                _doorCoroutine = StartCoroutine(OpenAndCloseDoor());
            }
        }

        private IEnumerator OpenAndCloseDoor()
        {
            _isOpen = true;
            // Play sound
            if (_openSound != null)
            {
                _audioSource.PlayOneShot(_openSound);
            }
            // Open door
            yield return RotateDoor(Quaternion.Euler(_closedRotation), Quaternion.Euler(_openRotation));
            // Wait
            yield return new WaitForSeconds(_openDuration);
            // Close door
            yield return RotateDoor(Quaternion.Euler(_openRotation), Quaternion.Euler(_closedRotation));
            _isOpen = false;
        }

        private IEnumerator RotateDoor(Quaternion from, Quaternion to)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * _openCloseSpeed;
                transform.localRotation = Quaternion.Slerp(from, to, elapsed);
                yield return null;
            }
            transform.localRotation = to;
        }
    }
}
