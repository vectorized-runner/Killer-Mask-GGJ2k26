using DG.Tweening;
using UnityEngine;

namespace KillerLocomotion
{
    public class KillerLocomotionController : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private AnimationClip[] _animations;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speed = 2f;
        
        private Sequence _movementSequence;
        private int _currentWaypointIndex;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartMovementLocomotion();
            }
            
            if(Input.GetKeyDown(KeyCode.Space))
            {
                ResetMovementLocomotion();
            }
        }
        
        private void StartMovementLocomotion()
        {
            _movementSequence = DOTween.Sequence();
            for (int i = 0; i < _waypoints.Length; i++)
            {
                int animIndex = i;
                _movementSequence.AppendCallback(() => {
                    if (_animations.Length > animIndex)
                    {
                        _animator.Play(_animations[animIndex].name);
                    }
                });
                _movementSequence.Append(transform.DOMove(_waypoints[i].position, _speed).SetEase(Ease.Linear));
                _movementSequence.Join(transform.DORotateQuaternion(_waypoints[i].rotation, _speed).SetEase(Ease.Linear));
            }
            _movementSequence.Play();
        }
        
        private void ResetMovementLocomotion()
        {
            _currentWaypointIndex = 0;
            transform.position = _waypoints[_currentWaypointIndex].position;
            transform.rotation = _waypoints[_currentWaypointIndex].rotation;
        }
    }
}
