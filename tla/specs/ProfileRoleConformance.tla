------------------------------ MODULE ProfileRoleConformance ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Claim:
* - profiles may list responsibility roles
* - profiles may not weaken the authorization boundary by permitting
*   authorize/acknowledge with roles outside the kernel-required roles
******************************************************************************)

CONSTANTS
  RequiredAuthorizationRole,
  RequiredAcknowledgmentRole,
  ProfileRoles,
  AuthorizationRequestRole,
  AcknowledgmentRequestRole

ASSUME
  /\ ProfileRoles /= {}

Inv_ProfileDeclaresAuthorizationRole ==
  RequiredAuthorizationRole \in ProfileRoles

Inv_ProfileDeclaresAcknowledgmentRole ==
  RequiredAcknowledgmentRole \in ProfileRoles

Inv_AuthorizationRoleMatchesBoundary ==
  AuthorizationRequestRole = RequiredAuthorizationRole

Inv_AcknowledgmentRoleMatchesBoundary ==
  AcknowledgmentRequestRole = RequiredAcknowledgmentRole

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
