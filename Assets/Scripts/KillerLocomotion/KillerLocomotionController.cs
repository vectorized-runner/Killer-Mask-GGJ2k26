using DG.Tweening;
using UnityEngine;

namespace KillerLocomotion
{
    public class KillerLocomotionController : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private Transform[] _waypoints_out;
        [SerializeField] private AnimationClip[] _animations;
        [SerializeField] private AnimationClip[] _animations_out;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speed = 2f;
        
        private Sequence _movementSequence;
        private int _currentWaypointIndex;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                StartInComingMovementLocomotion();
            }
            
            if(Input.GetKeyDown(KeyCode.O))
            {
                StartOutGoingMovementLocomotion();
            }
            
            if(Input.GetKeyDown(KeyCode.R))
            {
                ResetMovementLocomotion();
            }
        }
        
        private void StartInComingMovementLocomotion()
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
        
        private void StartOutGoingMovementLocomotion()
        {
            _movementSequence = DOTween.Sequence();
            for (int i = 0; i < _waypoints_out.Length; i++)
            {
                int animIndex = i;
                _movementSequence.AppendCallback(() => {
                    if (_animations_out.Length > animIndex)
                    {
                        _animator.Play(_animations_out[animIndex].name);
                    }
                });
                _movementSequence.Append(transform.DOMove(_waypoints_out[i].position, _speed).SetEase(Ease.Linear));
                _movementSequence.Join(transform.DORotateQuaternion(_waypoints_out[i].rotation, _speed).SetEase(Ease.Linear));
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
