
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEngine.InputSystem.Users;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(657461495204722195, typeof(InputUser))]
    public class InputUserTypeInterface:TypeInterface<InputUser>
    {
        static TypeMember[] _members = new TypeMember[]
            {
                (0,onChange),
                (1,onUnpairedDeviceUsed),
                (2,onPrefilterUnpairedDeviceActivity),
            };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string onChange => nameof(InputUser.onChange);
        static string onUnpairedDeviceUsed => nameof(InputUser.onUnpairedDeviceUsed);
        static string onPrefilterUnpairedDeviceActivity => nameof(InputUser.onPrefilterUnpairedDeviceActivity);
    }
}
