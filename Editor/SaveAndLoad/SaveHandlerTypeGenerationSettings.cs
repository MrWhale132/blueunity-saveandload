
using System.Collections.Generic;
using System.Reflection;
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

        public List<SaveHandlerTypeGenerationConfig> _configs;

        public Dictionary<MemberKey, MemberInclusionMode> _memberInclusionMode = new();
        public Dictionary<MemberKey, string> _memberDirective = new();
        public SaveHandlerAttributeGenerationSettings _attributeGenerationSettings;


        public SaveHandlerTypeGenerationSettings(List<SaveHandlerTypeGenerationConfig> typeGenerationConfigs = null)
        {
            _configs = typeGenerationConfigs;
        }

        public bool HasInclusionModeFor(MemberInfo member, out MemberInclusionMode inclusionMode)
        {
            return HasInclusionModeFor(MemberKey.From(member), out inclusionMode);
        }
        public bool HasInclusionModeFor(string methodId, out MemberInclusionMode inclusionMode)
        {
            return HasInclusionModeFor(MemberKey.From(methodId), out inclusionMode);
        }

        public bool HasInclusionModeFor(MemberKey member, out MemberInclusionMode inclusionMode)
        {
            if (_memberInclusionMode.TryGetValue(member, out inclusionMode))
            {
                return true;
            }
            inclusionMode = (MemberInclusionMode)(-1);
            return false;
        }


        public bool HasDirective(MemberInfo member, out string directive)
        {
            return HasDirective(MemberKey.From(member), out directive);
        }
        public bool HasDirective(string methodId, out string directive)
        {
            return HasDirective(MemberKey.From(methodId), out directive);
        }

        public bool HasDirective(MemberKey member, out string directive)
        {
            if (_memberDirective.TryGetValue(member, out directive))
            {
                return true;
            }
            directive = null;
            return false;
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


        public bool IsValid(bool logErrorMessages = false)
        {
            bool isValid = true;

            if (_configs != null)
            {
                var attributeSettings = new SaveHandlerAttributeGenerationSettings();
                bool hasAttributeSettings = false;


                foreach (var typeConfig in _configs)
                {
                    if(typeConfig._loadOrder != 0)
                    {
                        if (attributeSettings.loadOrder.HasValue)
                        {
                            if(attributeSettings.loadOrder.Value != typeConfig._loadOrder)
                            {
                                isValid = false;
                                if (logErrorMessages)
                                    Debug.LogError($"SaveHandlerTypeGenerationSettings: Conflicting load orders for type '{typeConfig.handlerIdOfConfiguredType}': '{attributeSettings.loadOrder.Value}' vs '{typeConfig._loadOrder}'.", typeConfig.logContext);
                            }
                        }
                        else
                        {
                            hasAttributeSettings = true;
                            attributeSettings.loadOrder = typeConfig._loadOrder;
                        }
                    }

                    
                    foreach (var memberConfig in typeConfig.memberConfigs)
                    {
                        MemberKey key;

                        if (memberConfig.methodId != 0)
                        {
                            key = MemberKey.From(memberConfig.methodId.ToString());
                        }
                        else
                        {
                            key = MemberKey.From(memberConfig.MemberInfo);
                        }


                        if (_memberInclusionMode.TryGetValue(key, out var existingInclusionMode))
                        {
                            if (existingInclusionMode != memberConfig.inclusionMode)
                            {
                                isValid = false;
                                if (logErrorMessages)
                                    Debug.LogError($"SaveHandlerTypeGenerationSettings: Conflicting inclusion modes for member '{memberConfig.memberName}': '{existingInclusionMode}' vs '{memberConfig.inclusionMode}'.", typeConfig.logContext);
                            }
                        }
                        else
                        {
                            _memberInclusionMode[key] = memberConfig.inclusionMode;
                        }


                        if (_memberDirective.TryGetValue(key, out var existingDirective))
                        {
                            _memberDirective[key] = existingDirective + " || " + memberConfig.directive;
                        }
                        else
                        {
                            _memberDirective[key] = memberConfig.directive;
                        }
                    }
                }

                if (hasAttributeSettings)
                    _attributeGenerationSettings = attributeSettings;
            }

            return isValid;
        }
    }



    public struct MemberKey
    {
        public string key;


        public MemberKey(MemberInfo member)
        {
            string underlyingKey = member.Name + " " + member.DeclaringType.AssemblyQualifiedName; //todo

            key = underlyingKey;
        }

        public MemberKey(string methodId)
        {
            key = methodId;
        }

        public static MemberKey From(MemberInfo member)
        {
            var key = new MemberKey(member);

            return key;
        }

        public static MemberKey From(string methodId)
        {
            var key = new MemberKey(methodId);

            return key;
        }


        public override bool Equals(object obj)
        {
            if (obj is MemberKey otherKey)
            {
                if (otherKey.key == key)
                    return true;

                return false;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }
    }
}
