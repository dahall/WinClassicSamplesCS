using System;
using System.Text;
using Vanara.Extensions;
using static Vanara.PInvoke.EnhancedStorage;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PortableDeviceApi;

namespace EhStorEnumerator
{
    //public enum PASSWD_INDICATOR
    //{
    //    PASSWD_INDICATOR_ADMIN,
    //    PASSWD_INDICATOR_USER,
    //}

    public class CCertProperties
    {
        public byte[] CertificateData;

        public CERT_TYPE nCertType = CERT_TYPE.CERT_TYPE_EMPTY;

        public uint nIndex = uint.MaxValue;

        public uint nNextCertIndex = uint.MaxValue;

        public CERT_TYPE nNextCertType = CERT_TYPE.CERT_TYPE_EMPTY;

        public uint nSignerCertIndex = uint.MaxValue;

        public CERT_VALIDATION_POLICY nValidationPolicy = CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_NONE;

        public string CertType => nCertType switch
        {
            CERT_TYPE.CERT_TYPE_EMPTY => "Empty",
            CERT_TYPE.CERT_TYPE_ASCm => "ASCm",
            CERT_TYPE.CERT_TYPE_PCp => "PCp",
            CERT_TYPE.CERT_TYPE_ASCh => "ASCh",
            CERT_TYPE.CERT_TYPE_HCh => "HCh",
            CERT_TYPE.CERT_TYPE_SIGNER => "SCh",
            _ => "Invalid",
        };

        public string ValidationPolicy => nValidationPolicy switch
        {
            CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_NONE => "None",
            CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_BASIC => "Basic",
            CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_EXTENDED => "Extended",
            _ => "Invalid",
        };
    }

    public static class CPortableDeviceImp
    {
        public static void CertDeviceAuthentication(this IPortableDevice device, int nCertificateIndex)
        {
            device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_DEVICE_CERTIFICATE_AUTHENTICATION, v =>
            {
                if (nCertificateIndex >= 0)
                    v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_INDEX, (uint)nCertificateIndex);
            });
        }

        public static CCertProperties CertGetCertificate(this IPortableDevice device, uint nCertIndex)
        {
            var results = device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_GET_CERTIFICATE, v => v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_INDEX, nCertIndex));
            results.GetBufferValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE, out var mem, out var nCertDataLen);
            return new CCertProperties()
            {
                CertificateData = mem.DangerousGetHandle().ToArray<byte>(nCertDataLen),
                nCertType = (CERT_TYPE)results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_TYPE),
                nValidationPolicy = (CERT_VALIDATION_POLICY)results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_VALIDATION_POLICY),
                nSignerCertIndex = results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_SIGNER_CERTIFICATE_INDEX),
                nNextCertIndex = results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_NEXT_CERTIFICATE_INDEX),
                nNextCertType = (CERT_TYPE)results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_NEXT_CERTIFICATE_OF_TYPE_INDEX),
            };
        }

        public static (uint nStoredCertCount, uint nMaxCertCount) CertGetCertificatesCount(this IPortableDevice device)
        {
            var results = device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_GET_CERTIFICATE_COUNT);
            return (results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_STORED_CERTIFICATE_COUNT), results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_MAX_CERTIFICATE_COUNT));
        }

        public static string[] CertGetSiloCapablity(this IPortableDevice device, CERT_CAPABILITY nCapablityType)
        {
            var results = device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_GET_SILO_CAPABILITY, v =>
            {
                v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_CAPABILITY_TYPE, (uint)nCapablityType);
            });
            results.GetBufferValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_SILO_CAPABILITY, out var mem, out var nDataLength);
            var pCapablityData = mem.ToArray<byte>(nDataLength);

            // pass one, calculate sizes
            uint nCapablityLen = 0, nMaxCapablityLen = 0, nCapablitiesCnt = 0;
            for (int nIndex = 1; nIndex < nDataLength; nIndex++)
            {
                if (pCapablityData[nIndex] != 0)
                {
                    nCapablityLen++;
                }
                else
                {
                    if (nCapablityLen != 0)
                    {
                        nMaxCapablityLen = (nCapablityLen > nMaxCapablityLen) ? nCapablityLen : nMaxCapablityLen;
                        nCapablitiesCnt++;
                        nCapablityLen = 0;
                    }
                }
            }

            // pass two, retreive data
            var ppCapablities = new string[nCapablitiesCnt];
            var szCapablity = new StringBuilder((int)nMaxCapablityLen);
            int nCapablityPtr = 0, nCapablities = 0;
            for (int nIndex = 1; nIndex < nDataLength; nIndex++)
            {
                if (pCapablityData[nIndex] != 0)
                {
                    szCapablity[nCapablityPtr++] = (char)pCapablityData[nIndex];
                    szCapablity[nCapablityPtr] = '\0';
                }
                else
                {
                    if (nCapablityPtr != 0)
                    {
                        ppCapablities[nCapablities++] = szCapablity.ToString();
                        nCapablityPtr = 0;
                    }
                }
            }

            return ppCapablities;
        }

        public static string CertGetSiloFriendlyName(this IPortableDevice device) =>
            GetCmdStr(device, ENHANCED_STORAGE_COMMAND_CERT_GET_ACT_FRIENDLY_NAME, ENHANCED_STORAGE_PROPERTY_CERTIFICATE_ACT_FRIENDLY_NAME);

        public static string CertGetSiloGUID(this IPortableDevice device) =>
            GetCmdStr(device, ENHANCED_STORAGE_COMMAND_CERT_GET_SILO_GUID, ENHANCED_STORAGE_PROPERTY_CERTIFICATE_SILO_GUID);

        public static string CertGetState(this IPortableDevice device)
        {
            var results = device.SendCmd(ENHANCED_STORAGE_COMMAND_SILO_GET_AUTHENTICATION_STATE);
            var state = (ENHANCED_STORAGE_AUTHN_STATE)results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_AUTHENTICATION_STATE);
            return state switch
            {
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_UNKNOWN => "Unknown",
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_NO_AUTHENTICATION_REQUIRED => "No Authentication Required",
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_NOT_AUTHENTICATED => "Not Authenticated",
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_AUTHENTICATED => "Authenticated",
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_AUTHENTICATION_DENIED => "Authentication Denied",
                ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_DEVICE_ERROR => "Device Error",
                _ => $"Invalid({(uint)state})",
            };
        }

        public static void CertHostAuthentication(this IPortableDevice device) => device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_HOST_CERTIFICATE_AUTHENTICATION);

        public static void CertInitializeToManufacturedState(this IPortableDevice device) => device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_INITIALIZE_TO_MANUFACTURER_STATE);

        public static void CertRemoveCertificate(this IPortableDevice device, uint nCertificateIndex) =>
            device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_SET_CERTIFICATE, v =>
            {
                v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_INDEX, nCertificateIndex);
                v.SetEnumValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_TYPE, CERT_TYPE.CERT_TYPE_EMPTY);
            });

        public static void CertSetCertificate(this IPortableDevice device, uint nCertIndex, CCertProperties certProperties)
        {
            device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_SET_CERTIFICATE, v =>
            {
                v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_INDEX, nCertIndex);
                v.SetBufferValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE, certProperties.CertificateData, certProperties.CertificateData.Length);
                v.SetEnumValue(ENHANCED_STORAGE_PROPERTY_CERTIFICATE_TYPE, certProperties.nCertType);
                v.SetEnumValue(ENHANCED_STORAGE_PROPERTY_VALIDATION_POLICY, certProperties.nValidationPolicy);
                v.SetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_SIGNER_CERTIFICATE_INDEX, certProperties.nSignerCertIndex);
            });
        }

        public static void SetEnumValue<T>(this IPortableDeviceValues vals, in PROPERTYKEY key, T enumVal) where T : Enum, IConvertible =>
            vals.SetUnsignedIntegerValue(key, enumVal.ToUInt32(null));

        public static void CertUnAuthentication(this IPortableDevice device) => device.SendCmd(ENHANCED_STORAGE_COMMAND_CERT_UNAUTHENTICATION);

        public static void PasswordChangePassword(this IPortableDevice device, bool isAdmin, string oldPassword, string newPassword, string passwordHint, string sid)
        {
            IPortableDeviceValues vals = new();
            vals.SetCommandPKey(ENHANCED_STORAGE_COMMAND_PASSWORD_CHANGE_PASSWORD);
            vals.SetBoolValue(ENHANCED_STORAGE_PROPERTY_PASSWORD_INDICATOR, !isAdmin);
            vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_PASSWORD, oldPassword);
            vals.SetBoolValue(ENHANCED_STORAGE_PROPERTY_NEW_PASSWORD_INDICATOR, !isAdmin);
            vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_NEW_PASSWORD, newPassword);
            vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_SECURITY_IDENTIFIER, sid);
            if (isAdmin)
                vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_ADMIN_HINT, passwordHint);
            else
                vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_USER_HINT, passwordHint);
            IPortableDeviceValues results = device.SendCommand(0, vals);
            results.GetErrorValue(WPD_PROPERTY_COMMON_HRESULT).ThrowIfFailed();
        }

        public static void PasswordInitializeToManufacturerState(this IPortableDevice device, string sid)
        {
            IPortableDeviceValues vals = new();
            vals.SetCommandPKey(ENHANCED_STORAGE_COMMAND_PASSWORD_START_INITIALIZE_TO_MANUFACTURER_STATE);
            if (!string.IsNullOrEmpty(sid))
            {
                vals.SetStringValue(ENHANCED_STORAGE_PROPERTY_SECURITY_IDENTIFIER, sid);
            }

            IPortableDeviceValues results = device.SendCommand(0, vals);
            results.GetErrorValue(WPD_PROPERTY_COMMON_HRESULT).ThrowIfFailed();
        }

        public static CPasswordSiloInformation PasswordQueryInformation(this IPortableDevice device) =>
            new(device.SendCmd(ENHANCED_STORAGE_COMMAND_PASSWORD_QUERY_INFORMATION));

        public static IPortableDeviceValues SendCmd(this IPortableDevice device, PROPERTYKEY cmd, Action<IPortableDeviceValues> addParams = null)
        {
            IPortableDeviceValues cmdParams = new();
            cmdParams.SetCommandPKey(cmd);
            addParams?.Invoke(cmdParams);
            var cmdResults = device.SendCommand(0, cmdParams);
            cmdResults.GetErrorValue(WPD_PROPERTY_COMMON_HRESULT).ThrowIfFailed();
            return cmdResults;
        }

        private static string GetCmdStr(IPortableDevice device, PROPERTYKEY cmd, PROPERTYKEY pkStr) =>
            device.SendCmd(cmd).GetStringValue(pkStr);
    }
}