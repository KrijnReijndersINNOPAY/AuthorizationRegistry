﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using NLIP.iShare.Api.Controllers;
using NLIP.iShare.Api.Filters;
using NLIP.iShare.AuthorizationRegistry.Api.Attributes;
using NLIP.iShare.AuthorizationRegistry.Core.Api;
using NLIP.iShare.IdentityServer.Delegation;
using NLIP.iShare.Models;
using NLIP.iShare.Models.DelegationEvidence;
using NLIP.iShare.Models.DelegationMask;
using System.Threading.Tasks;

namespace NLIP.iShare.AuthorizationRegistry.Api.Controllers
{
    [Route("delegation")]
    [TypeFilter(typeof(JsonSchemaValidateAttribute), Arguments = new object[]{ "delegationMaskSchema.json" })]
    public class DelegationEvidenceController : SchemeAuthorizedController
    {
        private readonly IDelegationService _delegationService;
        private readonly IDelegationTranslateService _delegationTranslateService;
        private readonly IDelegationMaskValidationService _delegationMaskValidationService;

        public DelegationEvidenceController(
            IDelegationService delegationService,
            IDelegationTranslateService delegationTranslateService,
            IDelegationMaskValidationService delegationMaskValidationService
        )
        {
            _delegationService = delegationService;
            _delegationTranslateService = delegationTranslateService;
            _delegationMaskValidationService = delegationMaskValidationService;
        }

        /// <summary>
        /// Obtains delegation evidence
        /// </summary>
        /// <remarks>
        /// Used to obtain delegation evidence from an Authorization Registry. The response is a signed JSON Web Token. 
        /// Please refer to the iSHARE language of delegation in order to understand the decoded response data model.
        /// </remarks>
        /// <param name="delegation_mask">iSHARE specific, optional, JSON structure that acts as a mask to delegation evidence</param>
        /// <returns>JWT encoded delegation evidence</returns>
        [HttpPost]
        [TypeFilter(typeof(AuthorizeDelegationRequestAttribute))]
        [SignResponse("delegation_token", "delegationEvidence", "DelegationEvidence", JsonContractType = typeof(CamelCasePropertyNamesContractResolver))]
        public async Task<ActionResult<DelegationEvidence>> Translate([FromBody]DelegationMask delegation_mask)
        {
            var result = await TranslateDelegation(delegation_mask);

            if (result.Value == null)
            {
                return result.Result;
            }

            return result.Value.DelegationEvidence;
        }

        /// <summary>
        /// Obtains delegation evidence
        /// </summary>
        /// <remarks>
        /// Used to obtain delegation evidence from an Authorization Registry. The response is a signed JSON Web Token. 
        /// Please refer to the iSHARE language of delegation in order to understand the decoded response data model.
        /// </remarks>
        /// <param name="delegation_mask">iSHARE specific, optional, JSON structure that acts as a mask to delegation evidence</param>
        /// <returns>The delegation evidence</returns>
        [HttpPost, Route("test"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<DelegationTranslationTestResponse>> TestDelegationTranslation([FromBody]DelegationMask delegation_mask)
        {
            return await TranslateDelegation(delegation_mask);
        }

        private async Task<ActionResult<DelegationTranslationTestResponse>> TranslateDelegation(DelegationMask delegationMask)
        {
            var validationResult = _delegationMaskValidationService.Validate(delegationMask);

            if (!validationResult.Success)
            {
                return BadRequest(new { error = validationResult.Error });
            }

            var delegation = await _delegationService
                .GetBySubject(delegationMask.DelegationRequest.Target.AccessSubject, delegationMask.DelegationRequest.PolicyIssuer)
                .ConfigureAwait(false);

            if (delegation == null)
            {
                return NotFound();
            }

            var delegationResponse = _delegationTranslateService.Translate(delegationMask, delegation.Policy);
            return delegationResponse;
        }
    }
}