--
-- PostgreSQL database dump
--

\restrict DdbTiesavUO1kL7Zgc4MD4RYoxc5PULViRY9nt2ZLUUmZTC2H4QvqEnXuR9pLjE

-- Dumped from database version 16.14
-- Dumped by pg_dump version 16.14

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AssessmentQuestions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AssessmentQuestions" (
    "Id" uuid NOT NULL,
    "CapabilityId" uuid NOT NULL,
    "Text" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "Code" text DEFAULT ''::text NOT NULL,
    "Provenance_MethodologyStatus" text,
    "Provenance_SourceClassification" integer DEFAULT 0 NOT NULL,
    "Provenance_SourceDocument" text DEFAULT ''::text NOT NULL,
    "Provenance_SourceSection" text,
    "SortOrder" integer DEFAULT 0 NOT NULL
);


--
-- Name: AssessmentResponses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AssessmentResponses" (
    "Id" uuid NOT NULL,
    "AssessmentId" uuid NOT NULL,
    "QuestionId" uuid NOT NULL,
    "SelectedMaturityLevelId" uuid,
    "RespondentComment" text,
    "ReviewerComment" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL,
    "AnswerState" integer DEFAULT 0 NOT NULL,
    "Confidence" integer,
    "EvidenceReferences" text[],
    "ReviewedMaturityLevelId" uuid,
    "CarriedForwardFromResponseId" uuid,
    "ConfirmedAtUtc" timestamp with time zone,
    "IsCarriedForward" boolean DEFAULT false NOT NULL
);


--
-- Name: AssessmentResults; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AssessmentResults" (
    "Id" uuid NOT NULL,
    "AssessmentId" uuid NOT NULL,
    "CalculatedAtUtc" timestamp with time zone NOT NULL,
    "CompositeAverage" numeric,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Assessments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Assessments" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL,
    "SubmittedAtUtc" timestamp with time zone,
    "SupersedesAssessmentId" uuid
);


--
-- Name: AuditEvents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AuditEvents" (
    "Id" uuid NOT NULL,
    "ActorUserId" uuid,
    "EventType" text NOT NULL,
    "EntityType" text NOT NULL,
    "EntityId" uuid NOT NULL,
    "PayloadJson" text,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Capabilities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Capabilities" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "SortOrder" integer DEFAULT 0 NOT NULL,
    "Code" text DEFAULT ''::text NOT NULL,
    "DimensionId" uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    "EvidenceGuidance" text,
    "Provenance_MethodologyStatus" text,
    "Provenance_SourceClassification" integer DEFAULT 0 NOT NULL,
    "Provenance_SourceDocument" text DEFAULT ''::text NOT NULL,
    "Provenance_SourceSection" text
);


--
-- Name: CapabilityScores; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."CapabilityScores" (
    "Id" uuid NOT NULL,
    "AssessmentResultId" uuid NOT NULL,
    "CapabilityId" uuid NOT NULL,
    "Score" numeric,
    "AnsweredQuestionCount" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: DimensionScores; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DimensionScores" (
    "Id" uuid NOT NULL,
    "AssessmentResultId" uuid NOT NULL,
    "DimensionId" uuid NOT NULL,
    "Score" numeric,
    "MaturityBand" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Dimensions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Dimensions" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "FundamentalQuestion" text,
    "SortOrder" integer NOT NULL,
    "Provenance_SourceDocument" text NOT NULL,
    "Provenance_SourceSection" text,
    "Provenance_SourceClassification" integer NOT NULL,
    "Provenance_MethodologyStatus" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: FrameworkVersions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."FrameworkVersions" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Version" text NOT NULL,
    "IsPublished" boolean NOT NULL,
    "PublishedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: InitiativeDependencies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."InitiativeDependencies" (
    "Id" uuid NOT NULL,
    "InitiativeId" uuid NOT NULL,
    "DependsOnInitiativeId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: InitiativeMilestones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."InitiativeMilestones" (
    "Id" uuid NOT NULL,
    "InitiativeId" uuid NOT NULL,
    "Title" text NOT NULL,
    "DueDate" timestamp with time zone,
    "SortOrder" integer NOT NULL,
    "IsDone" boolean NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Initiatives; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Initiatives" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "SourceFindingId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "Priority" integer NOT NULL,
    "Status" integer NOT NULL,
    "OwnerUserId" uuid,
    "ExpectedOutcome" text,
    "TargetStartDate" timestamp with time zone,
    "TargetCompletionDate" timestamp with time zone,
    "CompletedAtUtc" timestamp with time zone,
    "CreatedByUserId" uuid NOT NULL,
    "Version" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: IntelligenceDebtCategoryMappings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtCategoryMappings" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "DimensionId" uuid NOT NULL,
    "Category" integer NOT NULL,
    "Provenance_SourceDocument" text NOT NULL,
    "Provenance_SourceSection" text,
    "Provenance_SourceClassification" integer NOT NULL,
    "Provenance_MethodologyStatus" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: IntelligenceDebtDependencies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtDependencies" (
    "Id" uuid NOT NULL,
    "FindingId" uuid NOT NULL,
    "DependsOnFindingId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: IntelligenceDebtDetectionProvenances; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtDetectionProvenances" (
    "Id" uuid NOT NULL,
    "FindingId" uuid NOT NULL,
    "AssessmentId" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "CategoryMappingId" uuid NOT NULL,
    "SeverityMappingId" uuid NOT NULL,
    "DimensionId" uuid,
    "CapabilityId" uuid,
    "ObservedScore" numeric NOT NULL,
    "MaturityBand" text NOT NULL,
    "ThresholdUsed" numeric NOT NULL,
    "DetectedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: IntelligenceDebtEvidence; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtEvidence" (
    "Id" uuid NOT NULL,
    "FindingId" uuid NOT NULL,
    "EvidenceType" integer NOT NULL,
    "Description" text NOT NULL,
    "SourceReference" text,
    "AssessmentResponseId" uuid,
    "DocumentId" uuid,
    "ExternalUri" text,
    "AddedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: IntelligenceDebtFindings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtFindings" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "Category" integer NOT NULL,
    "Severity" integer NOT NULL,
    "Status" integer NOT NULL,
    "DetectionSource" integer NOT NULL,
    "BusinessImpact" text,
    "AffectedScope" text,
    "OwnerUserId" uuid,
    "TargetResolutionDate" timestamp with time zone,
    "AssessmentId" uuid,
    "CapabilityId" uuid,
    "DimensionId" uuid,
    "RecommendedAction" text,
    "RemediationPlan" text,
    "ValidationCriteria" text,
    "CreatedByUserId" uuid NOT NULL,
    "ApprovedAtUtc" timestamp with time zone,
    "ApprovedByUserId" uuid,
    "RemediationStartedAtUtc" timestamp with time zone,
    "ResolvedAtUtc" timestamp with time zone,
    "ValidatedAtUtc" timestamp with time zone,
    "ValidatedByUserId" uuid,
    "Outcome" text,
    "Version" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: IntelligenceDebtSeverityMappings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."IntelligenceDebtSeverityMappings" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "MaturityBandId" uuid NOT NULL,
    "Severity" integer NOT NULL,
    "Provenance_SourceDocument" text NOT NULL,
    "Provenance_SourceSection" text,
    "Provenance_SourceClassification" integer NOT NULL,
    "Provenance_MethodologyStatus" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: Invitations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Invitations" (
    "Id" uuid NOT NULL,
    "Email" text NOT NULL,
    "Role" integer NOT NULL,
    "InvitedByUserId" uuid NOT NULL,
    "ConsumedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: MaturityBands; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MaturityBands" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "Name" text NOT NULL,
    "MinScore" numeric NOT NULL,
    "MaxScore" numeric NOT NULL,
    "SortOrder" integer NOT NULL,
    "Provenance_SourceDocument" text NOT NULL,
    "Provenance_SourceSection" text,
    "Provenance_SourceClassification" integer NOT NULL,
    "Provenance_MethodologyStatus" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: MaturityLevels; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MaturityLevels" (
    "Id" uuid NOT NULL,
    "FrameworkVersionId" uuid NOT NULL,
    "Level" integer NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "Provenance_MethodologyStatus" text,
    "Provenance_SourceClassification" integer DEFAULT 0 NOT NULL,
    "Provenance_SourceDocument" text DEFAULT ''::text NOT NULL,
    "Provenance_SourceSection" text
);


--
-- Name: Memberships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Memberships" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "InvitedAtUtc" timestamp with time zone NOT NULL,
    "AcceptedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Organizations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Organizations" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Industry" text,
    "EmployeeCount" integer,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "TenantId" uuid NOT NULL
);


--
-- Name: Tenants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Tenants" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Slug" text NOT NULL,
    "TenantModel" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: Users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "EntraObjectId" text NOT NULL,
    "Email" text NOT NULL,
    "DisplayName" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: AssessmentQuestions PK_AssessmentQuestions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentQuestions"
    ADD CONSTRAINT "PK_AssessmentQuestions" PRIMARY KEY ("Id");


--
-- Name: AssessmentResponses PK_AssessmentResponses; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "PK_AssessmentResponses" PRIMARY KEY ("Id");


--
-- Name: AssessmentResults PK_AssessmentResults; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResults"
    ADD CONSTRAINT "PK_AssessmentResults" PRIMARY KEY ("Id");


--
-- Name: Assessments PK_Assessments; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Assessments"
    ADD CONSTRAINT "PK_Assessments" PRIMARY KEY ("Id");


--
-- Name: AuditEvents PK_AuditEvents; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AuditEvents"
    ADD CONSTRAINT "PK_AuditEvents" PRIMARY KEY ("Id");


--
-- Name: Capabilities PK_Capabilities; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Capabilities"
    ADD CONSTRAINT "PK_Capabilities" PRIMARY KEY ("Id");


--
-- Name: CapabilityScores PK_CapabilityScores; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CapabilityScores"
    ADD CONSTRAINT "PK_CapabilityScores" PRIMARY KEY ("Id");


--
-- Name: DimensionScores PK_DimensionScores; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DimensionScores"
    ADD CONSTRAINT "PK_DimensionScores" PRIMARY KEY ("Id");


--
-- Name: Dimensions PK_Dimensions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Dimensions"
    ADD CONSTRAINT "PK_Dimensions" PRIMARY KEY ("Id");


--
-- Name: FrameworkVersions PK_FrameworkVersions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FrameworkVersions"
    ADD CONSTRAINT "PK_FrameworkVersions" PRIMARY KEY ("Id");


--
-- Name: InitiativeDependencies PK_InitiativeDependencies; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiativeDependencies"
    ADD CONSTRAINT "PK_InitiativeDependencies" PRIMARY KEY ("Id");


--
-- Name: InitiativeMilestones PK_InitiativeMilestones; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiativeMilestones"
    ADD CONSTRAINT "PK_InitiativeMilestones" PRIMARY KEY ("Id");


--
-- Name: Initiatives PK_Initiatives; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Initiatives"
    ADD CONSTRAINT "PK_Initiatives" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtCategoryMappings PK_IntelligenceDebtCategoryMappings; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtCategoryMappings"
    ADD CONSTRAINT "PK_IntelligenceDebtCategoryMappings" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtDependencies PK_IntelligenceDebtDependencies; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDependencies"
    ADD CONSTRAINT "PK_IntelligenceDebtDependencies" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtDetectionProvenances PK_IntelligenceDebtDetectionProvenances; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "PK_IntelligenceDebtDetectionProvenances" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtEvidence PK_IntelligenceDebtEvidence; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtEvidence"
    ADD CONSTRAINT "PK_IntelligenceDebtEvidence" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtFindings PK_IntelligenceDebtFindings; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "PK_IntelligenceDebtFindings" PRIMARY KEY ("Id");


--
-- Name: IntelligenceDebtSeverityMappings PK_IntelligenceDebtSeverityMappings; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtSeverityMappings"
    ADD CONSTRAINT "PK_IntelligenceDebtSeverityMappings" PRIMARY KEY ("Id");


--
-- Name: Invitations PK_Invitations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Invitations"
    ADD CONSTRAINT "PK_Invitations" PRIMARY KEY ("Id");


--
-- Name: MaturityBands PK_MaturityBands; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaturityBands"
    ADD CONSTRAINT "PK_MaturityBands" PRIMARY KEY ("Id");


--
-- Name: MaturityLevels PK_MaturityLevels; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaturityLevels"
    ADD CONSTRAINT "PK_MaturityLevels" PRIMARY KEY ("Id");


--
-- Name: Memberships PK_Memberships; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Memberships"
    ADD CONSTRAINT "PK_Memberships" PRIMARY KEY ("Id");


--
-- Name: Organizations PK_Organizations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Organizations"
    ADD CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id");


--
-- Name: Tenants PK_Tenants; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tenants"
    ADD CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_AssessmentQuestions_CapabilityId_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AssessmentQuestions_CapabilityId_Code" ON public."AssessmentQuestions" USING btree ("CapabilityId", "Code");


--
-- Name: IX_AssessmentResponses_AssessmentId_QuestionId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AssessmentResponses_AssessmentId_QuestionId" ON public."AssessmentResponses" USING btree ("AssessmentId", "QuestionId");


--
-- Name: IX_AssessmentResponses_CarriedForwardFromResponseId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResponses_CarriedForwardFromResponseId" ON public."AssessmentResponses" USING btree ("CarriedForwardFromResponseId");


--
-- Name: IX_AssessmentResponses_QuestionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResponses_QuestionId" ON public."AssessmentResponses" USING btree ("QuestionId");


--
-- Name: IX_AssessmentResponses_ReviewedMaturityLevelId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResponses_ReviewedMaturityLevelId" ON public."AssessmentResponses" USING btree ("ReviewedMaturityLevelId");


--
-- Name: IX_AssessmentResponses_SelectedMaturityLevelId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResponses_SelectedMaturityLevelId" ON public."AssessmentResponses" USING btree ("SelectedMaturityLevelId");


--
-- Name: IX_AssessmentResponses_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResponses_TenantId" ON public."AssessmentResponses" USING btree ("TenantId");


--
-- Name: IX_AssessmentResults_AssessmentId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AssessmentResults_AssessmentId" ON public."AssessmentResults" USING btree ("AssessmentId");


--
-- Name: IX_AssessmentResults_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AssessmentResults_TenantId" ON public."AssessmentResults" USING btree ("TenantId");


--
-- Name: IX_Assessments_FrameworkVersionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Assessments_FrameworkVersionId" ON public."Assessments" USING btree ("FrameworkVersionId");


--
-- Name: IX_Assessments_OrganizationId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Assessments_OrganizationId" ON public."Assessments" USING btree ("OrganizationId");


--
-- Name: IX_Assessments_SupersedesAssessmentId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Assessments_SupersedesAssessmentId" ON public."Assessments" USING btree ("SupersedesAssessmentId") WHERE ("SupersedesAssessmentId" IS NOT NULL);


--
-- Name: IX_Assessments_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Assessments_TenantId" ON public."Assessments" USING btree ("TenantId");


--
-- Name: IX_AuditEvents_EntityType_EntityId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AuditEvents_EntityType_EntityId" ON public."AuditEvents" USING btree ("EntityType", "EntityId");


--
-- Name: IX_AuditEvents_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AuditEvents_TenantId" ON public."AuditEvents" USING btree ("TenantId");


--
-- Name: IX_Capabilities_DimensionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Capabilities_DimensionId" ON public."Capabilities" USING btree ("DimensionId");


--
-- Name: IX_Capabilities_FrameworkVersionId_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Capabilities_FrameworkVersionId_Code" ON public."Capabilities" USING btree ("FrameworkVersionId", "Code");


--
-- Name: IX_CapabilityScores_AssessmentResultId_CapabilityId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_CapabilityScores_AssessmentResultId_CapabilityId" ON public."CapabilityScores" USING btree ("AssessmentResultId", "CapabilityId");


--
-- Name: IX_CapabilityScores_CapabilityId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CapabilityScores_CapabilityId" ON public."CapabilityScores" USING btree ("CapabilityId");


--
-- Name: IX_CapabilityScores_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CapabilityScores_TenantId" ON public."CapabilityScores" USING btree ("TenantId");


--
-- Name: IX_DimensionScores_AssessmentResultId_DimensionId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_DimensionScores_AssessmentResultId_DimensionId" ON public."DimensionScores" USING btree ("AssessmentResultId", "DimensionId");


--
-- Name: IX_DimensionScores_DimensionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_DimensionScores_DimensionId" ON public."DimensionScores" USING btree ("DimensionId");


--
-- Name: IX_DimensionScores_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_DimensionScores_TenantId" ON public."DimensionScores" USING btree ("TenantId");


--
-- Name: IX_Dimensions_FrameworkVersionId_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Dimensions_FrameworkVersionId_Code" ON public."Dimensions" USING btree ("FrameworkVersionId", "Code");


--
-- Name: IX_FrameworkVersions_Name_Version; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_FrameworkVersions_Name_Version" ON public."FrameworkVersions" USING btree ("Name", "Version");


--
-- Name: IX_InitiativeDependencies_DependsOnInitiativeId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InitiativeDependencies_DependsOnInitiativeId" ON public."InitiativeDependencies" USING btree ("DependsOnInitiativeId");


--
-- Name: IX_InitiativeDependencies_InitiativeId_DependsOnInitiativeId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_InitiativeDependencies_InitiativeId_DependsOnInitiativeId" ON public."InitiativeDependencies" USING btree ("InitiativeId", "DependsOnInitiativeId");


--
-- Name: IX_InitiativeDependencies_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InitiativeDependencies_TenantId" ON public."InitiativeDependencies" USING btree ("TenantId");


--
-- Name: IX_InitiativeMilestones_InitiativeId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InitiativeMilestones_InitiativeId" ON public."InitiativeMilestones" USING btree ("InitiativeId");


--
-- Name: IX_InitiativeMilestones_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InitiativeMilestones_TenantId" ON public."InitiativeMilestones" USING btree ("TenantId");


--
-- Name: IX_Initiatives_CreatedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Initiatives_CreatedByUserId" ON public."Initiatives" USING btree ("CreatedByUserId");


--
-- Name: IX_Initiatives_OrganizationId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Initiatives_OrganizationId" ON public."Initiatives" USING btree ("OrganizationId");


--
-- Name: IX_Initiatives_OwnerUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Initiatives_OwnerUserId" ON public."Initiatives" USING btree ("OwnerUserId");


--
-- Name: IX_Initiatives_SourceFindingId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Initiatives_SourceFindingId" ON public."Initiatives" USING btree ("SourceFindingId");


--
-- Name: IX_Initiatives_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Initiatives_TenantId" ON public."Initiatives" USING btree ("TenantId");


--
-- Name: IX_Initiatives_TenantId_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Initiatives_TenantId_Code" ON public."Initiatives" USING btree ("TenantId", "Code");


--
-- Name: IX_IntelligenceDebtCategoryMappings_DimensionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtCategoryMappings_DimensionId" ON public."IntelligenceDebtCategoryMappings" USING btree ("DimensionId");


--
-- Name: IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~" ON public."IntelligenceDebtCategoryMappings" USING btree ("FrameworkVersionId", "DimensionId", "Category");


--
-- Name: IX_IntelligenceDebtDependencies_DependsOnFindingId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDependencies_DependsOnFindingId" ON public."IntelligenceDebtDependencies" USING btree ("DependsOnFindingId");


--
-- Name: IX_IntelligenceDebtDependencies_FindingId_DependsOnFindingId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_IntelligenceDebtDependencies_FindingId_DependsOnFindingId" ON public."IntelligenceDebtDependencies" USING btree ("FindingId", "DependsOnFindingId");


--
-- Name: IX_IntelligenceDebtDependencies_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDependencies_TenantId" ON public."IntelligenceDebtDependencies" USING btree ("TenantId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_AssessmentId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDetectionProvenances_AssessmentId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("AssessmentId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_CategoryMappingId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDetectionProvenances_CategoryMappingId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("CategoryMappingId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_FindingId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_IntelligenceDebtDetectionProvenances_FindingId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("FindingId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_FrameworkVersionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDetectionProvenances_FrameworkVersionId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("FrameworkVersionId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_SeverityMappingId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDetectionProvenances_SeverityMappingId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("SeverityMappingId");


--
-- Name: IX_IntelligenceDebtDetectionProvenances_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtDetectionProvenances_TenantId" ON public."IntelligenceDebtDetectionProvenances" USING btree ("TenantId");


--
-- Name: IX_IntelligenceDebtEvidence_AddedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtEvidence_AddedByUserId" ON public."IntelligenceDebtEvidence" USING btree ("AddedByUserId");


--
-- Name: IX_IntelligenceDebtEvidence_AssessmentResponseId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtEvidence_AssessmentResponseId" ON public."IntelligenceDebtEvidence" USING btree ("AssessmentResponseId");


--
-- Name: IX_IntelligenceDebtEvidence_FindingId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtEvidence_FindingId" ON public."IntelligenceDebtEvidence" USING btree ("FindingId");


--
-- Name: IX_IntelligenceDebtEvidence_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtEvidence_TenantId" ON public."IntelligenceDebtEvidence" USING btree ("TenantId");


--
-- Name: IX_IntelligenceDebtFindings_ApprovedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_ApprovedByUserId" ON public."IntelligenceDebtFindings" USING btree ("ApprovedByUserId");


--
-- Name: IX_IntelligenceDebtFindings_AssessmentId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_AssessmentId" ON public."IntelligenceDebtFindings" USING btree ("AssessmentId");


--
-- Name: IX_IntelligenceDebtFindings_CapabilityId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_CapabilityId" ON public."IntelligenceDebtFindings" USING btree ("CapabilityId");


--
-- Name: IX_IntelligenceDebtFindings_CreatedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_CreatedByUserId" ON public."IntelligenceDebtFindings" USING btree ("CreatedByUserId");


--
-- Name: IX_IntelligenceDebtFindings_DimensionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_DimensionId" ON public."IntelligenceDebtFindings" USING btree ("DimensionId");


--
-- Name: IX_IntelligenceDebtFindings_OrganizationId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_OrganizationId" ON public."IntelligenceDebtFindings" USING btree ("OrganizationId");


--
-- Name: IX_IntelligenceDebtFindings_OwnerUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_OwnerUserId" ON public."IntelligenceDebtFindings" USING btree ("OwnerUserId");


--
-- Name: IX_IntelligenceDebtFindings_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_TenantId" ON public."IntelligenceDebtFindings" USING btree ("TenantId");


--
-- Name: IX_IntelligenceDebtFindings_TenantId_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_IntelligenceDebtFindings_TenantId_Code" ON public."IntelligenceDebtFindings" USING btree ("TenantId", "Code");


--
-- Name: IX_IntelligenceDebtFindings_TenantId_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_TenantId_Status" ON public."IntelligenceDebtFindings" USING btree ("TenantId", "Status");


--
-- Name: IX_IntelligenceDebtFindings_ValidatedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtFindings_ValidatedByUserId" ON public."IntelligenceDebtFindings" USING btree ("ValidatedByUserId");


--
-- Name: IX_IntelligenceDebtSeverityMappings_FrameworkVersionId_Maturit~; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_IntelligenceDebtSeverityMappings_FrameworkVersionId_Maturit~" ON public."IntelligenceDebtSeverityMappings" USING btree ("FrameworkVersionId", "MaturityBandId");


--
-- Name: IX_IntelligenceDebtSeverityMappings_MaturityBandId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_IntelligenceDebtSeverityMappings_MaturityBandId" ON public."IntelligenceDebtSeverityMappings" USING btree ("MaturityBandId");


--
-- Name: IX_Invitations_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Invitations_TenantId" ON public."Invitations" USING btree ("TenantId");


--
-- Name: IX_Invitations_TenantId_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Invitations_TenantId_Email" ON public."Invitations" USING btree ("TenantId", "Email") WHERE ("ConsumedAtUtc" IS NULL);


--
-- Name: IX_MaturityBands_FrameworkVersionId_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_MaturityBands_FrameworkVersionId_Name" ON public."MaturityBands" USING btree ("FrameworkVersionId", "Name");


--
-- Name: IX_MaturityLevels_FrameworkVersionId_Level; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_MaturityLevels_FrameworkVersionId_Level" ON public."MaturityLevels" USING btree ("FrameworkVersionId", "Level");


--
-- Name: IX_Memberships_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Memberships_TenantId" ON public."Memberships" USING btree ("TenantId");


--
-- Name: IX_Memberships_TenantId_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Memberships_TenantId_UserId" ON public."Memberships" USING btree ("TenantId", "UserId");


--
-- Name: IX_Memberships_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Memberships_UserId" ON public."Memberships" USING btree ("UserId");


--
-- Name: IX_Organizations_TenantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Organizations_TenantId" ON public."Organizations" USING btree ("TenantId");


--
-- Name: IX_Tenants_Slug; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Tenants_Slug" ON public."Tenants" USING btree ("Slug");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: IX_Users_EntraObjectId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Users_EntraObjectId" ON public."Users" USING btree ("EntraObjectId");


--
-- Name: AssessmentQuestions FK_AssessmentQuestions_Capabilities_CapabilityId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentQuestions"
    ADD CONSTRAINT "FK_AssessmentQuestions_Capabilities_CapabilityId" FOREIGN KEY ("CapabilityId") REFERENCES public."Capabilities"("Id") ON DELETE RESTRICT;


--
-- Name: AssessmentResponses FK_AssessmentResponses_AssessmentQuestions_QuestionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "FK_AssessmentResponses_AssessmentQuestions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES public."AssessmentQuestions"("Id") ON DELETE RESTRICT;


--
-- Name: AssessmentResponses FK_AssessmentResponses_AssessmentResponses_CarriedForwardFromR~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "FK_AssessmentResponses_AssessmentResponses_CarriedForwardFromR~" FOREIGN KEY ("CarriedForwardFromResponseId") REFERENCES public."AssessmentResponses"("Id") ON DELETE RESTRICT;


--
-- Name: AssessmentResponses FK_AssessmentResponses_Assessments_AssessmentId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "FK_AssessmentResponses_Assessments_AssessmentId" FOREIGN KEY ("AssessmentId") REFERENCES public."Assessments"("Id") ON DELETE CASCADE;


--
-- Name: AssessmentResponses FK_AssessmentResponses_MaturityLevels_ReviewedMaturityLevelId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "FK_AssessmentResponses_MaturityLevels_ReviewedMaturityLevelId" FOREIGN KEY ("ReviewedMaturityLevelId") REFERENCES public."MaturityLevels"("Id") ON DELETE RESTRICT;


--
-- Name: AssessmentResponses FK_AssessmentResponses_MaturityLevels_SelectedMaturityLevelId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResponses"
    ADD CONSTRAINT "FK_AssessmentResponses_MaturityLevels_SelectedMaturityLevelId" FOREIGN KEY ("SelectedMaturityLevelId") REFERENCES public."MaturityLevels"("Id") ON DELETE RESTRICT;


--
-- Name: AssessmentResults FK_AssessmentResults_Assessments_AssessmentId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AssessmentResults"
    ADD CONSTRAINT "FK_AssessmentResults_Assessments_AssessmentId" FOREIGN KEY ("AssessmentId") REFERENCES public."Assessments"("Id") ON DELETE CASCADE;


--
-- Name: Assessments FK_Assessments_Assessments_SupersedesAssessmentId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Assessments"
    ADD CONSTRAINT "FK_Assessments_Assessments_SupersedesAssessmentId" FOREIGN KEY ("SupersedesAssessmentId") REFERENCES public."Assessments"("Id") ON DELETE RESTRICT;


--
-- Name: Assessments FK_Assessments_FrameworkVersions_FrameworkVersionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Assessments"
    ADD CONSTRAINT "FK_Assessments_FrameworkVersions_FrameworkVersionId" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: Assessments FK_Assessments_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Assessments"
    ADD CONSTRAINT "FK_Assessments_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Capabilities FK_Capabilities_Dimensions_DimensionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Capabilities"
    ADD CONSTRAINT "FK_Capabilities_Dimensions_DimensionId" FOREIGN KEY ("DimensionId") REFERENCES public."Dimensions"("Id") ON DELETE RESTRICT;


--
-- Name: Capabilities FK_Capabilities_FrameworkVersions_FrameworkVersionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Capabilities"
    ADD CONSTRAINT "FK_Capabilities_FrameworkVersions_FrameworkVersionId" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: CapabilityScores FK_CapabilityScores_AssessmentResults_AssessmentResultId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CapabilityScores"
    ADD CONSTRAINT "FK_CapabilityScores_AssessmentResults_AssessmentResultId" FOREIGN KEY ("AssessmentResultId") REFERENCES public."AssessmentResults"("Id") ON DELETE CASCADE;


--
-- Name: CapabilityScores FK_CapabilityScores_Capabilities_CapabilityId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CapabilityScores"
    ADD CONSTRAINT "FK_CapabilityScores_Capabilities_CapabilityId" FOREIGN KEY ("CapabilityId") REFERENCES public."Capabilities"("Id") ON DELETE RESTRICT;


--
-- Name: DimensionScores FK_DimensionScores_AssessmentResults_AssessmentResultId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DimensionScores"
    ADD CONSTRAINT "FK_DimensionScores_AssessmentResults_AssessmentResultId" FOREIGN KEY ("AssessmentResultId") REFERENCES public."AssessmentResults"("Id") ON DELETE CASCADE;


--
-- Name: DimensionScores FK_DimensionScores_Dimensions_DimensionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DimensionScores"
    ADD CONSTRAINT "FK_DimensionScores_Dimensions_DimensionId" FOREIGN KEY ("DimensionId") REFERENCES public."Dimensions"("Id") ON DELETE RESTRICT;


--
-- Name: Dimensions FK_Dimensions_FrameworkVersions_FrameworkVersionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Dimensions"
    ADD CONSTRAINT "FK_Dimensions_FrameworkVersions_FrameworkVersionId" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: InitiativeDependencies FK_InitiativeDependencies_Initiatives_DependsOnInitiativeId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiativeDependencies"
    ADD CONSTRAINT "FK_InitiativeDependencies_Initiatives_DependsOnInitiativeId" FOREIGN KEY ("DependsOnInitiativeId") REFERENCES public."Initiatives"("Id") ON DELETE RESTRICT;


--
-- Name: InitiativeDependencies FK_InitiativeDependencies_Initiatives_InitiativeId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiativeDependencies"
    ADD CONSTRAINT "FK_InitiativeDependencies_Initiatives_InitiativeId" FOREIGN KEY ("InitiativeId") REFERENCES public."Initiatives"("Id") ON DELETE RESTRICT;


--
-- Name: InitiativeMilestones FK_InitiativeMilestones_Initiatives_InitiativeId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiativeMilestones"
    ADD CONSTRAINT "FK_InitiativeMilestones_Initiatives_InitiativeId" FOREIGN KEY ("InitiativeId") REFERENCES public."Initiatives"("Id") ON DELETE CASCADE;


--
-- Name: Initiatives FK_Initiatives_IntelligenceDebtFindings_SourceFindingId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Initiatives"
    ADD CONSTRAINT "FK_Initiatives_IntelligenceDebtFindings_SourceFindingId" FOREIGN KEY ("SourceFindingId") REFERENCES public."IntelligenceDebtFindings"("Id") ON DELETE RESTRICT;


--
-- Name: Initiatives FK_Initiatives_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Initiatives"
    ADD CONSTRAINT "FK_Initiatives_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: Initiatives FK_Initiatives_Users_CreatedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Initiatives"
    ADD CONSTRAINT "FK_Initiatives_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: Initiatives FK_Initiatives_Users_OwnerUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Initiatives"
    ADD CONSTRAINT "FK_Initiatives_Users_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtCategoryMappings FK_IntelligenceDebtCategoryMappings_Dimensions_DimensionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtCategoryMappings"
    ADD CONSTRAINT "FK_IntelligenceDebtCategoryMappings_Dimensions_DimensionId" FOREIGN KEY ("DimensionId") REFERENCES public."Dimensions"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtCategoryMappings FK_IntelligenceDebtCategoryMappings_FrameworkVersions_Framewor~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtCategoryMappings"
    ADD CONSTRAINT "FK_IntelligenceDebtCategoryMappings_FrameworkVersions_Framewor~" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDependencies FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Depen~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDependencies"
    ADD CONSTRAINT "FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Depen~" FOREIGN KEY ("DependsOnFindingId") REFERENCES public."IntelligenceDebtFindings"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDependencies FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Findi~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDependencies"
    ADD CONSTRAINT "FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Findi~" FOREIGN KEY ("FindingId") REFERENCES public."IntelligenceDebtFindings"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDetectionProvenances FK_IntelligenceDebtDetectionProvenances_Assessments_Assessment~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "FK_IntelligenceDebtDetectionProvenances_Assessments_Assessment~" FOREIGN KEY ("AssessmentId") REFERENCES public."Assessments"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDetectionProvenances FK_IntelligenceDebtDetectionProvenances_FrameworkVersions_Fram~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "FK_IntelligenceDebtDetectionProvenances_FrameworkVersions_Fram~" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDetectionProvenances FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtCatego~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtCatego~" FOREIGN KEY ("CategoryMappingId") REFERENCES public."IntelligenceDebtCategoryMappings"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtDetectionProvenances FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtFindin~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtFindin~" FOREIGN KEY ("FindingId") REFERENCES public."IntelligenceDebtFindings"("Id") ON DELETE CASCADE;


--
-- Name: IntelligenceDebtDetectionProvenances FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtSeveri~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtDetectionProvenances"
    ADD CONSTRAINT "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtSeveri~" FOREIGN KEY ("SeverityMappingId") REFERENCES public."IntelligenceDebtSeverityMappings"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtEvidence FK_IntelligenceDebtEvidence_AssessmentResponses_AssessmentResp~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtEvidence"
    ADD CONSTRAINT "FK_IntelligenceDebtEvidence_AssessmentResponses_AssessmentResp~" FOREIGN KEY ("AssessmentResponseId") REFERENCES public."AssessmentResponses"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtEvidence FK_IntelligenceDebtEvidence_IntelligenceDebtFindings_FindingId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtEvidence"
    ADD CONSTRAINT "FK_IntelligenceDebtEvidence_IntelligenceDebtFindings_FindingId" FOREIGN KEY ("FindingId") REFERENCES public."IntelligenceDebtFindings"("Id") ON DELETE CASCADE;


--
-- Name: IntelligenceDebtEvidence FK_IntelligenceDebtEvidence_Users_AddedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtEvidence"
    ADD CONSTRAINT "FK_IntelligenceDebtEvidence_Users_AddedByUserId" FOREIGN KEY ("AddedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Assessments_AssessmentId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Assessments_AssessmentId" FOREIGN KEY ("AssessmentId") REFERENCES public."Assessments"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Capabilities_CapabilityId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Capabilities_CapabilityId" FOREIGN KEY ("CapabilityId") REFERENCES public."Capabilities"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Dimensions_DimensionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Dimensions_DimensionId" FOREIGN KEY ("DimensionId") REFERENCES public."Dimensions"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Organizations_OrganizationId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES public."Organizations"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Users_ApprovedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Users_ApprovedByUserId" FOREIGN KEY ("ApprovedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Users_CreatedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Users_OwnerUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Users_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtFindings FK_IntelligenceDebtFindings_Users_ValidatedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtFindings"
    ADD CONSTRAINT "FK_IntelligenceDebtFindings_Users_ValidatedByUserId" FOREIGN KEY ("ValidatedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtSeverityMappings FK_IntelligenceDebtSeverityMappings_FrameworkVersions_Framewor~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtSeverityMappings"
    ADD CONSTRAINT "FK_IntelligenceDebtSeverityMappings_FrameworkVersions_Framewor~" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: IntelligenceDebtSeverityMappings FK_IntelligenceDebtSeverityMappings_MaturityBands_MaturityBand~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."IntelligenceDebtSeverityMappings"
    ADD CONSTRAINT "FK_IntelligenceDebtSeverityMappings_MaturityBands_MaturityBand~" FOREIGN KEY ("MaturityBandId") REFERENCES public."MaturityBands"("Id") ON DELETE RESTRICT;


--
-- Name: MaturityBands FK_MaturityBands_FrameworkVersions_FrameworkVersionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaturityBands"
    ADD CONSTRAINT "FK_MaturityBands_FrameworkVersions_FrameworkVersionId" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: MaturityLevels FK_MaturityLevels_FrameworkVersions_FrameworkVersionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaturityLevels"
    ADD CONSTRAINT "FK_MaturityLevels_FrameworkVersions_FrameworkVersionId" FOREIGN KEY ("FrameworkVersionId") REFERENCES public."FrameworkVersions"("Id") ON DELETE RESTRICT;


--
-- Name: Memberships FK_Memberships_Tenants_TenantId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Memberships"
    ADD CONSTRAINT "FK_Memberships_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES public."Tenants"("Id") ON DELETE RESTRICT;


--
-- Name: Memberships FK_Memberships_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Memberships"
    ADD CONSTRAINT "FK_Memberships_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

\unrestrict DdbTiesavUO1kL7Zgc4MD4RYoxc5PULViRY9nt2ZLUUmZTC2H4QvqEnXuR9pLjE

