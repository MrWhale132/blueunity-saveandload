
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.UtilScripts;
using UnityEngine.InputSystem;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.UtilsScripts.Adapters
{

    public class InputActionAdapter
    {
        public InputAction __inputAction;
        public InputAction InputAction => __inputAction;


        public ActionAdapter<InputAction.CallbackContext> performed { get; set; } = new();
        public ActionAdapter<InputAction.CallbackContext> canceled { get; set; } = new();


        public InputActionAdapter()
        {

        }

        public InputActionAdapter(InputAction inputAction)
        {
            __inputAction = inputAction;

            __inputAction.performed += (callback) => performed.Invoke(callback);
            __inputAction.canceled += (callback) => canceled.Invoke(callback);
        }


        public void Enable()
        {
            __inputAction.Enable();
        }

        public TValue ReadValue<TValue>() where TValue : struct
        {
            TValue value = __inputAction.ReadValue<TValue>();
            //Debug.Log("read: " + value);
            return value;
        }
        public bool WasPressedThisFrame()
        {
            return __inputAction.WasPressedThisFrame();
        }
        public bool IsPressed()
        {
            return __inputAction.IsPressed();
        }

        



        [SaveHandler(234432942962789, nameof(InputActionAdapter), typeof(InputActionAdapter))]
        public class InputActionAdapterSaveHandler : UnmanagedSaveHandler<InputActionAdapter, InputActionAdapterSaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();

                __saveData.__inputAction = GetObjectId(__instance.__inputAction);
                __saveData.performed = GetObjectId(__instance.performed);
                __saveData.canceled = GetObjectId(__instance.canceled);
            }
            public override void LoadPhase1()
            {
                base.LoadPhase1();

                __saveData.__inputAction.AssignById(ref __instance.__inputAction);
                __instance.performed = Infra.Singleton.GetObjectById<ActionAdapter<InputAction.CallbackContext>>(__saveData.performed);
                __instance.canceled = Infra.Singleton.GetObjectById<ActionAdapter<InputAction.CallbackContext>>(__saveData.canceled);

                __instance.__inputAction.performed += Performed;
                __instance.__inputAction.canceled += Canceled;
            }


            void Performed(InputAction.CallbackContext context)
            {
                __instance.performed.Invoke(context);
            }

            void Canceled(InputAction.CallbackContext context)
            {
                __instance.canceled.Invoke(context);
            }

            public override void ReleaseObject()
            {
                __instance.__inputAction.performed -= Performed;
                __instance.__inputAction.canceled -= Canceled;

                base.ReleaseObject();
            }
        }

        public class InputActionAdapterSaveData : SaveDataBase
        {
            public RandomId __inputAction;
            public RandomId performed;
            public RandomId canceled;
        }
    }


    public static class InputActionAdapterExtensions
    {
        public static InputActionAdapter ToAdapter(this InputAction inputAction)
        {
            return new InputActionAdapter(inputAction);
        }
    }
}
