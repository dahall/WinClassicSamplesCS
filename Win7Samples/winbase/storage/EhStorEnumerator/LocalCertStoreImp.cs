using System;
using System.Collections.Generic;
using System.Text;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Crypt32;

namespace EhStorEnumerator
{
    public enum SYSTEM_STORE_NAMES
    {
        SYSTEM_STORE_CA,
        SYSTEM_STORE_MY,
        SYSTEM_STORE_ROOT,
        SYSTEM_STORE_SPC,
    }

    public class CCertificate
    {
        public IntPtr EncodedData;
        public string Issuer;
        public uint nEncodedLength;
        public string Subject;
        public uint Version;
        internal const CertEncodingType encType = CertEncodingType.X509_ASN_ENCODING | CertEncodingType.PKCS_7_ASN_ENCODING;
        private const CertNameStringFormat strFmt = CertNameStringFormat.CERT_X500_NAME_STR | CertNameStringFormat.CERT_NAME_STR_REVERSE_FLAG | CertNameStringFormat.CERT_NAME_STR_NO_QUOTING_FLAG;

        public CCertificate()
        {
        }

        public CCertificate(PCCERT_CONTEXT pCertContext)
        {
            unsafe
            {
                Issuer = GetDecodedCertName(((CERT_INFO*)((CERT_CONTEXT*)pCertContext)->pCertInfo)->Issuer, strFmt);
                Subject = GetDecodedCertName(((CERT_INFO*)((CERT_CONTEXT*)pCertContext)->pCertInfo)->Subject, strFmt);
                EncodedData = ((CERT_CONTEXT*)pCertContext)->pbCertEncoded;
                nEncodedLength = ((CERT_CONTEXT*)pCertContext)->cbCertEncoded;
                Version = ((CERT_INFO*)((CERT_CONTEXT*)pCertContext)->pCertInfo)->dwVersion;
            }
        }

        public byte[] GetEncodedData() => EncodedData.ToArray<byte>((int)nEncodedLength);

        private static string GetDecodedCertName(in CRYPTOAPI_BLOB pCertName, CertNameStringFormat dwStrType)
        {
            // find the length first
            uint dwDataLength = CertNameToStr(encType, pCertName, dwStrType, default, 0);
            if (dwDataLength == 0)
            {
                throw new Exception();
            }

            StringBuilder pString = new((int)dwDataLength);
            _ = CertNameToStr(encType, pCertName, dwStrType, pString, dwDataLength);
            return pString.ToString();
        }
    }

    public class CLocalCertStoreImp
    {
        private readonly SafeHCERTSTORE m_hCertStoreHandle = new(default, false);

        public CLocalCertStoreImp(SYSTEM_STORE_NAMES nStore)
        {
            string szSubsystemProtocol = nStore.ToString()["SYSTEM_STORE_".Length..];
            Win32Error.ThrowLastErrorIfInvalid(m_hCertStoreHandle = CertOpenSystemStore(default, szSubsystemProtocol));
        }

        public void AddCertificate(CCertificate certificate)
        {
            if (!CertAddEncodedCertificateToStore(m_hCertStoreHandle, CCertificate.encType,
                certificate.EncodedData, certificate.nEncodedLength, CertStoreAdd.CERT_STORE_ADD_NEW, default))
            {
                Win32Error.ThrowLastError();
            }
        }

        public List<CCertificate> GetCertificatesList()
        {
            List<CCertificate> parCertificates = new();
            PCCERT_CONTEXT pCertContext = default;
            while (!(pCertContext = CertEnumCertificatesInStore(m_hCertStoreHandle, pCertContext)).IsNull)
            {
                parCertificates.Add(new(pCertContext));
            }

            return parCertificates;
        }
    }
}