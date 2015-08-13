﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Xml;

namespace Servicios
{
    internal class TokenValidationHandler : DelegatingHandler
    {
        // Domain name or Tenant name 
        const string domainName = "desarrolloarkix.onmicrosoft.com";
        const string audience = "92c60ccd-3fd1-48f6-a2c0-4f7f63a31668";

        static DateTime _stsMetadataRetrievalTime = DateTime.MinValue;
        static List<X509SecurityToken> _signingTokens = null;
        static string _issuer = string.Empty;


        // SendAsync is used to validate incoming requests contain a valid access token, and sets the current user identity  
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string jwtToken;
            string issuer;
            List<X509SecurityToken> signingTokens;

            if (!TryRetrieveToken(request, out jwtToken))
            {
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            try
            {
                // Get tenant information that's used to validate incoming jwt tokens 
                GetTenantInformation(string.Format("https://login.windows.net/{0}/federationmetadata/2007-06/federationmetadata.xml", domainName), out issuer, out signingTokens);
            }
            catch (Exception)
            {
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler()
            {
                Configuration = new SecurityTokenHandlerConfiguration
                {
                    CertificateValidator = X509CertificateValidator.None,

                }
                //CertificateValidator = X509CertificateValidator.None

            };

            System.IdentityModel.Tokens.TokenValidationParameters validationParameters = new System.IdentityModel.Tokens.TokenValidationParameters()
            {
                ValidAudience = audience,
                ValidIssuer = issuer,
                IssuerSigningTokens = signingTokens

                //AllowedAudience = audience,
                //ValidIssuer = issuer,
                //SigningTokens = signingTokens
            };



            try
            {
                SecurityToken token = null;
                //// Validate token 
                ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out token);

                ////set the ClaimsPrincipal on the current thread. 
                Thread.CurrentPrincipal = claimsPrincipal;

                // set the ClaimsPrincipal on HttpContext.Current if the app is running in web hosted environment. 
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.User = claimsPrincipal;
                }

                // Verify that required permission is set in the scope claim 
                Claim scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
                if (scopeClaim != null)
                {
                    if (scopeClaim.Value != "user_impersonation")
                    {
                        throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
                    }
                }

                return base.SendAsync(request, cancellationToken);
            }
            catch (SecurityTokenValidationException)
            {
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }
            catch (Exception)
            {
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
        }

        // Reads the token from the authorization header on the incoming request 
        private static bool TryRetrieveToken(HttpRequestMessage request, out string token)
        {
            token = null;
            string authzHeader;

            if (!request.Headers.Contains("Authorization"))
            {
                return false;
            }

            authzHeader = request.Headers.GetValues("Authorization").First<string>();

            // Verify Authorization header contains 'Bearer' scheme 
            token = authzHeader.StartsWith("Bearer ") ? authzHeader.Split(' ')[1] : null;

            if (null == token)
            {
                return false;
            }

            return true;
        }

        /// <summary> 
        /// Parses the federation metadata document and gets issuer Name and Signing Certificates 
        /// </summary> 
        /// <param name="metadataAddress">URL of the Federation Metadata document</param> 
        /// <param name="issuer">Issuer Name</param> 
        /// <param name="signingTokens">Signing Certificates in the form of X509SecurityToken</param> 
        static void GetTenantInformation(string metadataAddress, out string issuer, out List<X509SecurityToken> signingTokens)
        {
            signingTokens = new List<X509SecurityToken>();

            // The issuer and signingTokens are cached for 24 hours. They are updated if any of the conditions in the if condition is true.             
            if (DateTime.UtcNow.Subtract(_stsMetadataRetrievalTime).TotalHours > 24
                || string.IsNullOrEmpty(_issuer)
                || _signingTokens == null)
            {
                MetadataSerializer serializer = new MetadataSerializer()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None
                };
                MetadataBase metadata = serializer.ReadMetadata(XmlReader.Create(metadataAddress));

                EntityDescriptor entityDescriptor = (EntityDescriptor)metadata;

                // get the issuer name 
                if (!string.IsNullOrWhiteSpace(entityDescriptor.EntityId.Id))
                {
                    _issuer = entityDescriptor.EntityId.Id;
                }

                // get the signing certs 
                _signingTokens = ReadSigningCertsFromMetadata(entityDescriptor);

                _stsMetadataRetrievalTime = DateTime.UtcNow;
            }

            issuer = _issuer;
            signingTokens = _signingTokens;
        }

        static List<X509SecurityToken> ReadSigningCertsFromMetadata(EntityDescriptor entityDescriptor)
        {
            List<X509SecurityToken> stsSigningTokens = new List<X509SecurityToken>();

            SecurityTokenServiceDescriptor stsd = entityDescriptor.RoleDescriptors.OfType<SecurityTokenServiceDescriptor>().First();

            if (stsd != null && stsd.Keys != null)
            {
                IEnumerable<X509RawDataKeyIdentifierClause> x509DataClauses = stsd.Keys.Where(key => key.KeyInfo != null && (key.Use == KeyType.Signing || key.Use == KeyType.Unspecified)).
                                                             Select(key => key.KeyInfo.OfType<X509RawDataKeyIdentifierClause>().First());

                stsSigningTokens.AddRange(x509DataClauses.Select(clause => new X509SecurityToken(new X509Certificate2(clause.GetX509RawData()))));
            }
            else
            {
                throw new InvalidOperationException("There is no RoleDescriptor of type SecurityTokenServiceType in the metadata");
            }

            return stsSigningTokens;
        }
    }
}
