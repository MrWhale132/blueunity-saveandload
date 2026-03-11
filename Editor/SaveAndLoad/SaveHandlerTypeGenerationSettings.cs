using Theblueway.Core.Runtime.Extensions;
using System.Collections.Generic;
using System.Reflection;
using Theblueway.CodeGen.Runtime;
using Theblueway.SaveAndLoad.Editor;
using UnityEngine;

namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad
{
    public class SaveHandlerAttributeGenerationSettings
    {
        public int? loadOrder;
    }

    public class SaveHandlerTypeGenerationSettings
    {
        public class MemberSettings
        {
            public string memberName;

            public MemberInclusionMode inclusionMode = MemberInclusionMode.Include;
            public string directive;

            public bool DirectiveIsSet => !string.IsNullOrEmpty(directive);
        }

        public Dictionary<string, MemberSettings> _memberSettingsByMemberName = new();


        public SaveHandlerAttributeGenerationSettings _attributeGenerationSettings;



        public static SaveHandlerTypeGenerationSettings From(List<SaveHandlerTypeGenerationConfiguration> configs)
        {
            Dictionary<string, MemberSettings> memberSettingsByName = new();
            Dictionary<string, Object> logContextesByMemberName = new();


            var attributeSettings = new SaveHandlerAttributeGenerationSettings();
            bool hasAttributeSettings = false;

            foreach (var config in configs)
            {
                foreach (var memberConfig in config._memberConfigs)
                {
                    if (memberSettingsByName.TryGetValue(memberConfig.memberName, out var memberSettings))
                    {
                        Debug.LogError($"Multiple savehandler type gen configs try to configure the same member. First wins, rest is ignored.\n" +
                            $"Member name: {memberConfig.memberName}", config.logContext);

                        Debug.LogError("First config that added this member:", logContextesByMemberName[memberConfig.memberName]);
                        continue;
                    }

                    memberSettings = new MemberSettings
                    {
                        memberName = memberConfig.memberName,
                        inclusionMode = memberConfig.inclusionMode,
                        directive = memberConfig.directive,
                    };

                    memberSettingsByName.Add(memberConfig.memberName, memberSettings);
                    logContextesByMemberName.Add(memberConfig.memberName, config.logContext);
                }


                if (config._loadOrder != 0)
                {
                    if (attributeSettings.loadOrder.HasValue)
                    {
                        if (attributeSettings.loadOrder.Value != config._loadOrder)
                        {
                                Debug.LogError($"SaveHandlerTypeGenerationSettings: Conflicting load orders for type {config.ConfiguredType.CleanAssemblyQualifiedName()}: '{attributeSettings.loadOrder.Value}' vs '{config._loadOrder}'.", config.logContext);
                        }
                    }
                    else
                    {
                        hasAttributeSettings = true;
                        attributeSettings.loadOrder = config._loadOrder;
                    }
                }

            }


            var settings = new SaveHandlerTypeGenerationSettings
            {
                _memberSettingsByMemberName = memberSettingsByName,
                _attributeGenerationSettings = hasAttributeSettings ? attributeSettings : null,
            };

            return settings;
        }


        public bool HasInclusionModeFor(MemberInfo member, out MemberInclusionMode inclusionMode)
        {
            string key = member.Name;

            if(member is MethodInfo methodInfo)
            {
                key = TypeUtils.GetMethodSignature(methodInfo);
            }

            if(_memberSettingsByMemberName.TryGetValue(key, out var memberSettings))
            {
                inclusionMode = memberSettings.inclusionMode;
                return true;
            }
            else
            {
                inclusionMode = (MemberInclusionMode)(-1);
                return false;
            }
        }



        public bool HasDirective(MemberInfo member, out string directive)
        {
            string key = member.Name;

            if (member is MethodInfo methodInfo)
            {
                key = TypeUtils.GetMethodSignature(methodInfo);
            }

            if (_memberSettingsByMemberName.TryGetValue(key, out var memberSettings) && memberSettings.DirectiveIsSet)
            {
                directive = memberSettings.directive;
                return true;
            }
            else
            {
                directive = null;
                return false;
            }
        }


        public bool HasAttributeGenerationSettings(out SaveHandlerAttributeGenerationSettings settings)
        {
            if (_attributeGenerationSettings != null)
            {
                settings = _attributeGenerationSettings;
                return true;
            }
            settings = null;
            return false;
        }
    }


    public enum MemberInclusionMode
    {
        Include,
        Exclude,
    }
}
