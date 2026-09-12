using CMS.Util.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoverWithDrag : MonoBehaviour
{
    private enum RotateType
    {
        CamRotate, ObjectRotate,
    }

    private enum HoldAxis
    {
        Release, Hold,
    }

    private enum ObjectRotateMode
    {
        Mode01, Mode02
    }

    [SerializeField] private RotateType rotateType;
    [SerializeField] private HoldAxis verticalHold = HoldAxis.Release;
    [SerializeField] private HoldAxis horizontalHold = HoldAxis.Release;
    [SerializeField] private bool useWheel = false;
    [SerializeField] private bool useUpDownWASD = false;
    [SerializeField] private ObjectRotateMode objectRotateMode = ObjectRotateMode.Mode01;

    public static string ZoomRequest = string.Empty;
    public static int MouseRotatingEvent = 0;

    public float sensitivity = 10f;

    Vector2 _CUR_MOUSE_POS = new Vector2(0, 0);
    Vector2 _CUR_POS = new Vector2(0, 0);

    bool _TOUCH_DOWN_FLAG = false;

    Rect _VALID_AREA = new Rect();

    public Transform axisTargetTransfrom = null;
    public Transform rotateTargetTransfrom = null;

    public float limitAngle = 180.0f;

    private float mouseX;
    private float mouseY;
    public float startZ = 0;

    public void SetTarget(Transform target)
    {
        if (rotateType == RotateType.CamRotate)
            axisTargetTransfrom = target;

        else
            rotateTargetTransfrom = target;
    }
    void Start()
    {
        _VALID_AREA.width = Screen.width * 0.8f;
        _VALID_AREA.height = Screen.height * 0.7f;
        _VALID_AREA.x = Screen.width * 0.2f;
        _VALID_AREA.y = Screen.height * 0.1f;
    }

    void LateUpdate()
    {
        if (!string.IsNullOrEmpty(CameraMoverWithDrag.ZoomRequest))
        {
            if (CameraMoverWithDrag.ZoomRequest == "ZoomIn")
            {
                transform.position += transform.forward * 100.0f * Time.deltaTime * 1;
            }
            else if (CameraMoverWithDrag.ZoomRequest == "ZoomOut")
            {
                transform.position += transform.forward * 100.0f * Time.deltaTime * -1;
            }

            CameraMoverWithDrag.ZoomRequest = string.Empty;
        }

        if (Input.GetMouseButton(0))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                #region KeyCode.LeftShift

                /*
                //카메라를 중심으로 회전하는 코드
                currentRotation.x += Input.GetAxis("Mouse X") * sensitivity;
                currentRotation.y -= Input.GetAxis("Mouse Y") * sensitivity;
                currentRotation.x = Mathf.Repeat(currentRotation.x, 360);
                currentRotation.y = Mathf.Clamp(currentRotation.y, -maxYAngle, maxYAngle);

                gameObject.transform.rotation = Quaternion.Euler(currentRotation.y + _x_offet, currentRotation.x + _y_offet, 0);
                */

                //위 아래 이동
                float x_offset = Input.GetAxis("Mouse X");
                float y_offset = Input.GetAxis("Mouse Y");

                float abs_x = Mathf.Abs(x_offset);
                float abs_y = Mathf.Abs(y_offset);


                if (abs_x > 0 || abs_y > 0)
                {
                    if (abs_x > abs_y)
                    {
                        transform.position += transform.right * -2.0f * x_offset * sensitivity * Time.deltaTime;
                    }
                    else
                    {
                        transform.position += transform.up * -2.0f * y_offset * sensitivity * Time.deltaTime;
                    }
                }

                #endregion KeyCode.LeftShift
            }
            else
            {
                //선택된 물체(원점)를 중심으로 회전

                if (_TOUCH_DOWN_FLAG == false)
                {
                    //마우스 누를 때
                    _CUR_MOUSE_POS.x = Input.mousePosition.x;
                    _CUR_MOUSE_POS.y = Screen.height - Input.mousePosition.y;
                }
                else
                {
                    //마우스 드래그
                    _CUR_POS.x = Input.mousePosition.x;
                    _CUR_POS.y = Screen.height - Input.mousePosition.y;

                    if (_VALID_AREA.Contains(_CUR_POS))
                    {
                        float x_offset = _CUR_POS.x - _CUR_MOUSE_POS.x;
                        float y_offset = _CUR_POS.y - _CUR_MOUSE_POS.y;

                        float abs_x = Mathf.Abs(x_offset);
                        float abs_y = Mathf.Abs(y_offset);

                        if (rotateType == RotateType.CamRotate)
                        {
                            if (abs_x > 0 || abs_y > 0)
                            {
                                if (abs_x > abs_y)
                                {
                                    if (axisTargetTransfrom == null && horizontalHold == HoldAxis.Release)
                                        gameObject.transform.RotateAround(Vector3.zero, Vector3.up, 1 * x_offset * sensitivity * Time.deltaTime);

                                    else
                                        gameObject.transform.RotateAround(axisTargetTransfrom.position, Vector3.up, 1 * x_offset * sensitivity * Time.deltaTime);
                                }

                                else
                                {
                                    if (axisTargetTransfrom == null && verticalHold == HoldAxis.Release)
                                        gameObject.transform.RotateAround(Vector3.zero, gameObject.transform.right, 1 * y_offset * sensitivity * Time.deltaTime);

                                    else
                                        gameObject.transform.RotateAround(axisTargetTransfrom.position, gameObject.transform.right, 1 * y_offset * sensitivity * Time.deltaTime);
                                }
                            }
                        }

                        else
                        {
                            if (abs_x > 0 || abs_y > 0)
                            {
                                if (objectRotateMode == ObjectRotateMode.Mode01)
                                {
                                    if (abs_x > abs_y)
                                    {
                                        if (rotateTargetTransfrom != null && horizontalHold == HoldAxis.Release)
                                            rotateTargetTransfrom.Rotate(Matrix4x4.Rotate(rotateTargetTransfrom.rotation).inverse.MultiplyPoint(Vector3.up), -1 * x_offset * sensitivity * Time.deltaTime);
                                    }

                                    else
                                    {
                                        if (rotateTargetTransfrom != null && verticalHold == HoldAxis.Release)
                                            rotateTargetTransfrom.Rotate(Matrix4x4.Rotate(rotateTargetTransfrom.rotation).inverse.MultiplyPoint(Vector3.right), -1 * y_offset * sensitivity * Time.deltaTime);
                                    }
                                }

                                else
                                {    
                                    //mouseX += Input.GetAxis("Mouse X") * sensitivity * 0.2f; // AxisX = Mouse Y
                                    //mouseY = Mathf.Clamp(mouseY + (Input.GetAxis("Mouse Y") * sensitivity * 0.2f), -limitAngle, limitAngle);
                                    
                                    //rotateTargetTransfrom.rotation = Quaternion.Euler(rotateTargetTransfrom.rotation.x + mouseY, rotateTargetTransfrom.rotation.y - mouseX, startZ);

                                    rotateTargetTransfrom.Rotate(Input.GetAxis("Mouse Y") * sensitivity * 0.2f, -Input.GetAxis("Mouse X") * sensitivity * 0.2f, 0f, Space.World);
                                    //rotateTargetTransfrom.Rotate(-Input.GetAxis("Mouse Y") * sensitivity * 0.2f, 0f, 0f);
                                }
                            }
                        }
                    }

                    _CUR_MOUSE_POS.x = _CUR_POS.x;
                    _CUR_MOUSE_POS.y = _CUR_POS.y;
                }

                _TOUCH_DOWN_FLAG = true;
            }

        }
        else
        {
            if (_TOUCH_DOWN_FLAG)
            {
                //마우스 뗄 때
            }

            _TOUCH_DOWN_FLAG = false;
        }

        if(useWheel)
            transform.position += transform.forward * 500.0f * Time.deltaTime * Input.GetAxis("Mouse ScrollWheel");

        if (useUpDownWASD)
        {
            if (Input.GetKey(KeyCode.W))
            {
                transform.position += transform.forward * 10.0f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                transform.position += transform.forward * -10.0f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position += transform.right * -10.0f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position += transform.right * 10.0f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += transform.up * 10.0f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position += transform.up * -10.0f * Time.deltaTime;
            }
        }
    }
}
