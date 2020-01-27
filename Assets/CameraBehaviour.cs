using System;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Assets
{
    public class CameraBehaviour : MonoBehaviour
    {
        private const float MainSpeed = 100.0f; //regular speed
        private const float VerticalSpeed = 100.0f;
        private const float ShiftAdd = 250.0f; //multiplied by how the key is held.  Basically running
        private const float MaxShift = 1000.0f; //Maximum speed when holdin
        private const float CamSens = 0.25f; //How sensitive it with mouse

        private bool _moving = false;
        private bool _fastMode = false;
        private Vector3 _lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float _totalRun = 1.0f;

        public Action<Vector3> OnPositionChanged;

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !_moving)
            {
                _moving = true;
                _lastMouse = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && _moving) _moving = false;

            if (Input.GetKeyDown(KeyCode.C)) _fastMode = !_fastMode;

            if (_moving)
            {
                _lastMouse = Input.mousePosition - _lastMouse;
                _lastMouse = new Vector3(-_lastMouse.y * CamSens, _lastMouse.x * CamSens, 0);
                _lastMouse = new Vector3(transform.eulerAngles.x + _lastMouse.x, transform.eulerAngles.y + _lastMouse.y, 0);
                transform.eulerAngles = _lastMouse;
                _lastMouse = Input.mousePosition;
            }

            //Keyboard commands
            float d = Time.deltaTime;
            Vector3 p = GetBaseInput();
            if( p == Vector3.zero ) _totalRun = Mathf.Clamp(_totalRun * 0.5f, 1f, 1000f);
            if (_fastMode)
            {
                _totalRun += d;
                p = p * _totalRun * ShiftAdd;
                p.x = Mathf.Clamp(p.x, -MaxShift, MaxShift);
                p.y = Mathf.Clamp(p.y, -MaxShift, MaxShift);
                p.z = Mathf.Clamp(p.z, -MaxShift, MaxShift);
            }
            else
            {
            
                p *= MainSpeed;
            }

            p *= d;
            Vector3 newPosition = transform.position;
            transform.Translate(p);
            if (Input.GetKey(KeyCode.Space)) newPosition.y += VerticalSpeed * d;
            if (Input.GetKey(KeyCode.LeftShift)) newPosition.y -= VerticalSpeed * d;

            newPosition.x = transform.position.x;
            newPosition.z = transform.position.z;
            if (newPosition == transform.position) return;
            transform.position = newPosition;
            OnPositionChanged?.Invoke( newPosition );
        }

        private static Vector3 GetBaseInput()
        {
            Vector3 v = Vector3.zero;
            //returns the basic values, if it's 0 than it's not active.
            if (Input.GetKey(KeyCode.W)) v += new Vector3(0, 0, 1);
            if (Input.GetKey(KeyCode.S)) v += new Vector3(0, 0, -1);
            if (Input.GetKey(KeyCode.A)) v += new Vector3(-1, 0, 0);
            if (Input.GetKey(KeyCode.D)) v += new Vector3(1, 0, 0);
            return v;
        }
    }
}



