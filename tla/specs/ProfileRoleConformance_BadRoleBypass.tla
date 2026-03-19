------------------------------ MODULE ProfileRoleConformance_BadRoleBypass ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Negative model: a profile or request path weakens the boundary by omitting
* a kernel-required role and allowing mismatched authorize/acknowledge roles.
******************************************************************************)

CONSTANTS
  RequiredAuthorizationRole,
  RequiredAcknowledgmentRole,
  ProfileRolesBad,
  AuthorizationRequestRoleBad,
  AcknowledgmentRequestRoleBad

ASSUME
  /\ ProfileRolesBad /= {}

Inv_ProfileDeclaresAuthorizationRole ==
  RequiredAuthorizationRole \in ProfileRolesBad

Inv_ProfileDeclaresAcknowledgmentRole ==
  RequiredAcknowledgmentRole \in ProfileRolesBad

Inv_AuthorizationRoleMatchesBoundary ==
  AuthorizationRequestRoleBad = RequiredAuthorizationRole

Inv_AcknowledgmentRoleMatchesBoundary ==
  AcknowledgmentRequestRoleBad = RequiredAcknowledgmentRole

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
