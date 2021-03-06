﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
//using Windows.Devices.Enumeration;
//using Windows.Devices.Scanners;

// used by SqlDependencyEx
// This file needs Case revision

namespace CMMManager
{

    public enum IllnessOption { Select, Close, Cancel };
    public enum IncidentOption { Select, Close, Cancel };

    public partial class frmCMMManager : Form
    {

        private int nLoggedUserId;

        private SqlConnection connRN;
        private String rn_cnn_str;
        private SqlCommand rn_cmd;

        //private SqlDependencyEx dependency;

        //String strSalesforceConnString;
        //SqlConnection connSalesforce;
        //SqlCommand cmdSalesforce;

        private SqlConnection connSalesforce;
        private String connStringSalesforce;
        private SqlCommand cmd_Salesforce;

        private Boolean bIsModified = false;

        public IndividualInfo IndividualSearched;

        // Delegates for Cross thread method call
        delegate void SetTabPages(int nIndex);

        delegate void RemoveRowInGVSettlement(int nRow);
        delegate void RemoveAllRowsInSettlement();
        delegate void AddRowToGVSettlement(DataGridViewRow row);

        delegate void RemoveMedBillInCase(int nRow);
        delegate void RemoveAllMedBillInCase();
        delegate void AddRowToMedBillInCase(DataGridViewRow row);

        delegate void RemoveCaseInProcess(int nRow);
        delegate void RemoveAllCaseInProcess();
        delegate void AddRowToCaseInProcess(DataGridViewRow row);

        delegate void RemoveCaseInCaseView(int nRow);
        delegate void RemoveAllCaseInCaseView();
        delegate void AddRowToCaseInCaseView(DataGridViewRow row);

        delegate void RemoveAllMedBillsInCaseEdit();
        delegate void AddRowToMedBillsInCaseEdit(DataGridViewRow row);

        delegate void SetBalaceMedBill(Decimal Balance);

        private enum TabPage { None, DashBoard, Individual, CaseView, Case, MedBill };
        private enum MedBillStatus { Pending, CMMPendingPayment, Closed, Ineligible, PartiallyIneligible };

        private TabPage BeforePrevTabPage = TabPage.None;
        private TabPage PrevTabPage = TabPage.None;
        private TabPage CurrentTabPage = TabPage.None;

        private TabPage MedBillStart = TabPage.None;

        // Enumerations
        private enum CaseMode { AddNew, Edit };
        private enum MedBillMode { AddNew, Edit };

        private CaseMode caseMode = CaseMode.AddNew;
        private MedBillMode medbillMode = MedBillMode.AddNew;

        private enum MedBillType { MedicalBill = 1, Prescription, PhysicalTherapy };
        //private enum MedBillStatus { PendingStatus, JobAssigned, InProgress, EligibilityReview, UnderBillProcessing, CompletedAndClose };
        private enum PatientType { OutPatient, InPatient };
        private enum Program { GoldPlus, Gold, Silver, Bronze, GoldMedi_I, GoldMedi_2 };
        private enum SettlementType { SelfPayDiscount = 1, ThirdPartyDiscount, MemberPayment, CMMProviderPayment, CMMDiscount, MemberReimbursement, Ineligible, MedicalProviderRefund, PersonalResponsibility };
        private enum PaymentMethodType { None, Check, CreditCard, ACH_Bankng };

        private enum SettlementMode { AddNew, Edit };

        private SettlementMode settlementMode = SettlementMode.Edit;

        private List<ICD10CodeInfo> lstICD10CodeInfo;

        //private String strConnStringForIllness;
        //private SqlConnection connIllness;


        //private String strNewMedBillNo;
        private String MedicalBillNo = String.Empty;
        private String strIndividualId = String.Empty;
        private String IndividualIdIndividualPage = String.Empty;
        //private String strSqlCreateCase = String.Empty;
        public String strCaseId = String.Empty;
        //public String CaseIdSelected = String.Empty;
        public String strCaseNameSelected = String.Empty;

        // Individual Id and Case ID for Med Bills in Case for editing
        String IndividualIdSelected = String.Empty;
        String CaseIdSelected = String.Empty;
        String CaseNameSelected = String.Empty;
        String CaseIdForCasePageMedBill = String.Empty;


        public String strCaseIdSelected = String.Empty;
        public String strContactIdSelected = String.Empty;

        public String strCaseIdForIllness = String.Empty;
        //public String strIndividualID = String.Empty;

        public Decimal PersonalResponsibilityAmountInMedBill;

        public Dictionary<int, String> dicMedBillTypes;
        public Dictionary<int, String> dicMedBillStatus;
        public Dictionary<int, String> dicPendingReason;
        public Dictionary<int, String> dicIneligibleReason;

        public List<CaseInfo> lstCaseInfo;
        public List<StaffInfo> lstCreateStaff;
        public List<StaffInfo> lstModifiStaff;

        public List<PaymentMethod> lstPaymentMethod;
        public List<CreditCardInfo> lstCreditCardInfo;
        public List<MedicalProviderInfo> lstMedicalProvider;
        public List<ChurchInfo> lstChurchInfo;
        public List<MedBillStatusInfo> lstMedBillStatusInfo;
        public List<SettlementTypeInfo> lstSettlementType;
        public List<PersonalResponsiblityTypeInfo> lstPersonalResponsibilityType;
        public List<MedBillNoteTypeInfo> lstMedBillNoteTypeInfo;
        public List<IncidentProgramInfo> lstIncidentProgramInfo;
        public SelectedIllness Illness;
        public SelectedIncident Incident;
        

        private String strNPFFormFileName = String.Empty;
        private String strIBFileName = String.Empty;
        private String strPoPFileName = String.Empty;
        private String strMedicalRecordFileName = String.Empty;
        private String strUnknownDocFileName = String.Empty;

        private String strNPFormFilePathSource = String.Empty;
        private String strNPFormFilePathDestination = String.Empty;

        private String strIBFilePathSource = String.Empty;
        private String strIBFilePathDestination = String.Empty;

        private String strPoPFilePathSource = String.Empty;
        private String strPopFilePathDestination = String.Empty;

        private String strMedRecordFilePathSource = String.Empty;
        private String strMedRecordFilePathDestination = String.Empty;

        private String strUnknownDocFilePathSource = String.Empty;
        private String strUnknownDocFilePathDestination = String.Empty;

        private String strDestinationPath = @"\\cmm-2014u\Sharefolder\";

        // Temporary storage for Medical Bill Information
        // Prescription fields
        private String tmpPrescriptionName = String.Empty;
        private String tmpPrescriptionDescription = String.Empty;
        private String tmpPrescriptionNote = String.Empty;
        private String tmpNumberOfMedication = String.Empty;

        // Physical Therapy fields
        private String tmpNumPhysicalTherapy = String.Empty;
        private String tmpPhysicalTherapyRxNote = String.Empty;

        // Medical Bill fields
        private Boolean tmpInPatient = false;
        private Boolean tmpOutPatient = false;
        private String tmpMedBillNote = String.Empty;
        private int tmpPendingReason;
        private int tmpIneligibleReason;

        public frmCMMManager()
        {
            InitializeComponent();

            //Control.CheckForIllegalCrossThreadCalls = false;
            Control.CheckForIllegalCrossThreadCalls = true;

            IndividualSearched = new IndividualInfo();

            lstICD10CodeInfo = new List<ICD10CodeInfo>();

            //rn_cnn_str = @"Data Source=CMM-2014U\CMM; Initial Catalog=RN_DB;Integrated Security=True";
            rn_cnn_str = @"Data Source=CMM-2014U\CMM; Initial Catalog=RN_DB;Integrated Security=True";

            connRN = new SqlConnection(rn_cnn_str);

            connStringSalesforce = @"Data Source=CMM-2014U\CMM; Initial Catalog=SalesForce; Integrated Security=True";
            connSalesforce = new SqlConnection(connStringSalesforce);

            SqlDependency.Start(rn_cnn_str);

            dicMedBillTypes = new Dictionary<int, String>();
            dicMedBillStatus = new Dictionary<int, String>();
            dicPendingReason = new Dictionary<int, String>();
            dicIneligibleReason = new Dictionary<int, String>();

            lstCreateStaff = new List<StaffInfo>();
            lstModifiStaff = new List<StaffInfo>();
            lstCaseInfo = new List<CaseInfo>();
            lstPaymentMethod = new List<PaymentMethod>();
            lstCreditCardInfo = new List<CreditCardInfo>();
            lstMedBillStatusInfo = new List<MedBillStatusInfo>();
            lstSettlementType = new List<SettlementTypeInfo>();
            lstPersonalResponsibilityType = new List<PersonalResponsiblityTypeInfo>();
            lstMedBillNoteTypeInfo = new List<MedBillNoteTypeInfo>();

            //strNewMedBillNo = "MEDBILL-0150000";

            Illness = new SelectedIllness();
            Incident = new SelectedIncident();
            // Medical Provider list
            lstMedicalProvider = new List<MedicalProviderInfo>();
            lstChurchInfo = new List<ChurchInfo>();
            lstIncidentProgramInfo = new List<IncidentProgramInfo>();

            //strDestinationPath = @"\\cmm-2014u\Sharefolder\" + DateTime.Today.ToString();

            String strSqlQueryForChurchInfo = "select [dbo].[Church].[ID], [dbo].[Church].[Name] from [dbo].[Church]";

            SqlCommand cmdQueryForChurchInfo = new SqlCommand(strSqlQueryForChurchInfo, connSalesforce);

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            SqlDataReader rdrChurchInfo = cmdQueryForChurchInfo.ExecuteReader();
            if (rdrChurchInfo.HasRows)
            {
                while (rdrChurchInfo.Read())
                {
                    if (!rdrChurchInfo.IsDBNull(0))
                    {
                        lstChurchInfo.Add(new ChurchInfo { ID = rdrChurchInfo.GetString(0), Name = rdrChurchInfo.GetString(1) });
                    }
                }
            }

            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            dicMedBillStatus.Clear();

            String strSqlQueryForMedBillStatus = "select [dbo].[tbl_medbill_status_code].[BillStatusCode], [dbo].[tbl_medbill_status_code].[BillStatusValue] " +
                                                 "from [dbo].[tbl_medbill_status_code]";

            SqlCommand cmdQueryForMedBillStatus = new SqlCommand(strSqlQueryForMedBillStatus, connRN);
            cmdQueryForMedBillStatus.CommandType = CommandType.Text;

            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            SqlDataReader rdrMedBillStatus = cmdQueryForMedBillStatus.ExecuteReader();
            if (rdrMedBillStatus.HasRows)
            {
                while(rdrMedBillStatus.Read())
                {
                    dicMedBillStatus.Add(rdrMedBillStatus.GetInt16(0), rdrMedBillStatus.GetString(1));
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Populate Pending Reason
            /// 
            String strSqlQueryForPendingReason = "select [dbo].[tbl_pending_reason].[id], [dbo].[tbl_pending_reason].[name] from [dbo].[tbl_pending_reason] " +
                                                 "order by [dbo].[tbl_pending_reason].[id]";

            SqlCommand cmdQueryForPendingReason = new SqlCommand(strSqlQueryForPendingReason, connRN);
            cmdQueryForPendingReason.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            SqlDataReader rdrPendingReason = cmdQueryForPendingReason.ExecuteReader();
            dicPendingReason.Clear();
            if (rdrPendingReason.HasRows)
            {
                while (rdrPendingReason.Read())
                {
                    if (!rdrPendingReason.IsDBNull(1)) dicPendingReason.Add(rdrPendingReason.GetInt32(0), rdrPendingReason.GetString(1));
                    else dicPendingReason.Add(rdrPendingReason.GetInt32(0), String.Empty);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Populate Ineligible Reason
            /// 
            String strSqlQueryForIneligibleReason = "select [dbo].[tbl_ineligible_reason].[id], [dbo].[tbl_ineligible_reason].[name] from [dbo].[tbl_ineligible_reason] " +
                                                    "order by [dbo].[tbl_ineligible_reason].[id]";

            SqlCommand cmdQueryForIneligibleReason = new SqlCommand(strSqlQueryForIneligibleReason, connRN);
            cmdQueryForIneligibleReason.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            SqlDataReader rdrIneligibleReason = cmdQueryForIneligibleReason.ExecuteReader();
            dicIneligibleReason.Clear();
            if (rdrIneligibleReason.HasRows)
            {
                while (rdrIneligibleReason.Read())
                {
                    if (!rdrIneligibleReason.IsDBNull(1)) dicIneligibleReason.Add(rdrIneligibleReason.GetInt32(0), rdrIneligibleReason.GetString(1));
                    else dicIneligibleReason.Add(rdrIneligibleReason.GetInt32(0), String.Empty);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();


            PersonalResponsibilityAmountInMedBill = 0;

            // Retrieve payment method
            lstPaymentMethod.Clear();
            String strSqlQueryForPaymentMethod = "select [dbo].[tbl_payment_method].[PaymentMethod_Id], [dbo].[tbl_payment_method].[PaymentMethod_Value] from [dbo].[tbl_payment_method] " +
                                                 "order by [dbo].[tbl_payment_method].[PaymentMethod_Value]";

            SqlCommand cmdQueryForPaymentMethod = new SqlCommand(strSqlQueryForPaymentMethod, connRN);
            cmdQueryForPaymentMethod.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            SqlDataReader rdrPaymentMethod = cmdQueryForPaymentMethod.ExecuteReader();
            if (rdrPaymentMethod.HasRows)
            {
                while (rdrPaymentMethod.Read())
                {
                    if (!rdrPaymentMethod.IsDBNull(1)) lstPaymentMethod.Add(new PaymentMethod { PaymentMethodId = rdrPaymentMethod.GetInt16(0), PaymentMethodValue = rdrPaymentMethod.GetString(1) });
                    else lstPaymentMethod.Add(new PaymentMethod { PaymentMethodId = rdrPaymentMethod.GetInt16(0), PaymentMethodValue = String.Empty });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            // Retrieve credit card info
            lstCreditCardInfo.Clear();
            String strSqlQueryForCreditCardInfo = "select [dbo].[tbl_Credit_Card__c].[CreditCard_Id], [dbo].[tbl_Credit_Card__c].[Name] from [dbo].[tbl_Credit_Card__c]";

            SqlCommand cmdQueryForCreditCardInfo = new SqlCommand(strSqlQueryForCreditCardInfo, connRN);
            cmdQueryForCreditCardInfo.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            SqlDataReader rdrCreditCardInfo = cmdQueryForCreditCardInfo.ExecuteReader();
            if (rdrCreditCardInfo.HasRows)
            {
                while (rdrCreditCardInfo.Read())
                {
                    if (!rdrCreditCardInfo.IsDBNull(1)) lstCreditCardInfo.Add(new CreditCardInfo { CreditCardId = rdrCreditCardInfo.GetInt16(0), CreditCardNo = rdrCreditCardInfo.GetString(1) });
                    else lstCreditCardInfo.Add(new CreditCardInfo { CreditCardId = rdrCreditCardInfo.GetInt16(0), CreditCardNo = null });
                }
                //lstCreditCardInfo.Add(new CreditCardInfo { CreditCardId = 0, CreditCardNo = "None" });
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();


            // retrieve settlement types from data base
            String strSqlQuerySettlementTypes = "select [dbo].[tbl_settlement_type_code].[SettlementTypeCode], [dbo].[tbl_settlement_type_code].[SettlementTypeValue] from [dbo].[tbl_settlement_type_code]";

            SqlCommand cmdQueryForSettlementType = new SqlCommand(strSqlQuerySettlementTypes, connRN);
            cmdQueryForSettlementType.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrSettlementType = cmdQueryForSettlementType.ExecuteReader();
            lstSettlementType.Clear();
            if (rdrSettlementType.HasRows)
            {
                while (rdrSettlementType.Read())
                {
                    if (!rdrSettlementType.IsDBNull(0) &&
                        !rdrSettlementType.IsDBNull(1))
                        lstSettlementType.Add(new SettlementTypeInfo { SettlementTypeCode = rdrSettlementType.GetInt16(0), SettlementTypeValue = rdrSettlementType.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            // retrieve personal responsibility types
            // lstPersonalResponsibilityType
            String strSqlQueryForPersonalResponsibilityTypes = "select [dbo].[tbl_personal_responsibility_code].[PersonalResponsibilityTypeCode], " +
                                                               "[dbo].[tbl_personal_responsibility_code].[PersonalResponsibilityTypeValue] " +
                                                               "from [dbo].[tbl_personal_responsibility_code]";

            SqlCommand cmdQueryForPersonalResponsibilityTypes = new SqlCommand(strSqlQueryForPersonalResponsibilityTypes, connRN);
            cmdQueryForPersonalResponsibilityTypes.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrPersonalResponsibilityTypes = cmdQueryForPersonalResponsibilityTypes.ExecuteReader();
            lstPersonalResponsibilityType.Clear();
            if (rdrPersonalResponsibilityTypes.HasRows)
            {
                while (rdrPersonalResponsibilityTypes.Read())
                {
                    if (!rdrPersonalResponsibilityTypes.IsDBNull(0) &&
                        !rdrPersonalResponsibilityTypes.IsDBNull(1))
                        lstPersonalResponsibilityType.Add(new PersonalResponsiblityTypeInfo
                        {
                            PersonalResponsibilityTypeCode = rdrPersonalResponsibilityTypes.GetInt16(0),
                            PersonalResponsibilityTypeValue = rdrPersonalResponsibilityTypes.GetString(1)
                        });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        ~frmCMMManager()
        {
            SqlDependency.Stop(rn_cnn_str);
            //dependency.Stop();

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult dlgClosing = MessageBox.Show("Do you want to exit?", "Comfirmation", MessageBoxButtons.YesNo);

            if (dlgClosing == DialogResult.Yes)
            {
                Close();
            }
            else if (dlgClosing == DialogResult.No)
            {
                return;
            }
        }

        private void frmCMMManager_Load(object sender, EventArgs e)
        {
            tbCMMManager.TabPages.Remove(tbpgDashboardFDManager);
            tbCMMManager.TabPages.Remove(tbpgDashboardFDStaff);
            tbCMMManager.TabPages.Remove(tbpgDashboardNPManager);
            tbCMMManager.TabPages.Remove(tbpgDashboardNPStaff);
            tbCMMManager.TabPages.Remove(tbpgDashboardRNManager);
            tbCMMManager.TabPages.Remove(tbpgIndividual);
            tbCMMManager.TabPages.Remove(tbpgCaseView);
            tbCMMManager.TabPages.Remove(tbpgCreateCase);
            tbCMMManager.TabPages.Remove(tbpgMedicalBill);
            tbCMMManager.TabPages.Remove(tbpgIllness);

            frmLogin frmLogin = new frmLogin();
            frmLogin.StartPosition = FormStartPosition.CenterParent;

            Boolean bLoginSuccess = false;

            for (int i = 0; i < 3; i++)
            {
                DialogResult loginResult = frmLogin.ShowDialog();

                if (loginResult == DialogResult.OK)
                {
                    bLoginSuccess = true;
                    nLoggedUserId = frmLogin.nLoggedUserId;
                    break;
                }
                else if (loginResult == DialogResult.Cancel)
                {
                    MessageBox.Show("Login Canceled", "Alert");
                    break;
                }
                else if (loginResult == DialogResult.Retry)
                {
                    continue;
                }
            }

            if (bLoginSuccess == false) Close();

            // Browse buttons for Case Creation tab
            if (bLoginSuccess == true)
            {
                ToolTip tipBrowseForNPF = new ToolTip();
                tipBrowseForNPF.SetToolTip(btnBrowseNPF, "Browse for Needs Processing Form");

                ToolTip tipBrowseForIB = new ToolTip();
                tipBrowseForIB.SetToolTip(btnBrowseIB, "Browse for Itemized Bill");

                ToolTip tipBrowseForPoP = new ToolTip();
                tipBrowseForPoP.SetToolTip(btnBrowsePoP, "Browse for Proof of Payment");

                ToolTip tipBrowseForMedRcord = new ToolTip();
                tipBrowseForMedRcord.SetToolTip(btnBrowseMR, "Browse for Medical Record");

                ToolTip tipBrowseForUnknownDoc = new ToolTip();
                tipBrowseForUnknownDoc.SetToolTip(btnBrowseUnknownDoc, "Browse for Unknown Document");

                // Upload buttons for Case Creation tab
                ToolTip tipUploadNPF = new ToolTip();
                tipUploadNPF.SetToolTip(btnNPFFormUpload, "Upload the Needs Processing Form to the server");

                ToolTip tipUploadIB = new ToolTip();
                tipUploadIB.SetToolTip(btnIBUpload, "Upload the Itemized Bill to the server");

                ToolTip tipUploadPoP = new ToolTip();
                tipUploadPoP.SetToolTip(btnPoPUpload, "Upload the Proof of Payment to the server");

                ToolTip tipUploadMedRec = new ToolTip();
                tipUploadMedRec.SetToolTip(btnMedicalRecordUpload, "Upload the Medical Record to the server");

                ToolTip tipUploadUnknownDoc = new ToolTip();
                tipUploadUnknownDoc.SetToolTip(btnUnknownDocUpload, "Upload the Unknown Document to the server");

                // View buttons for Case Creation tab
                ToolTip tipViewNPFCreateCase = new ToolTip();
                tipViewNPFCreateCase.SetToolTip(btnNPFFormView, "View the NPF Form on the server");

                ToolTip tipViewIBCreateCase = new ToolTip();
                tipViewIBCreateCase.SetToolTip(btnIBView, "View the Itemized Bill on the server");

                ToolTip tipViewPoPCreateCase = new ToolTip();
                tipViewPoPCreateCase.SetToolTip(btnPoPView, "View the Proof of Payment on the server");

                ToolTip tipViewMedRecCreateCase = new ToolTip();
                tipViewMedRecCreateCase.SetToolTip(btnMedicalRecordView, "View the Medical Record Document on the server");

                ToolTip tipViewUnknownDocCreateCase = new ToolTip();
                tipViewUnknownDocCreateCase.SetToolTip(btnOtherDocView, "View the Unknown Document on the server");


                // Delete buttons for Case Creation tab
                ToolTip tipDeleteNPF = new ToolTip();
                tipDeleteNPF.SetToolTip(btnNPFFormDelete, "Delete the uploaded Needs Processing Form on the server");

                ToolTip tipDeleteIB = new ToolTip();
                tipDeleteIB.SetToolTip(btnDeleteIB, "Delete the uploaded Itemized Bill on the server");

                ToolTip tipDeletePoP = new ToolTip();
                tipDeletePoP.SetToolTip(btnDeletePoP, "Delete the uploaded Proof of Payment on the server");

                ToolTip tipDeleteMedRec = new ToolTip();
                tipDeleteMedRec.SetToolTip(btnDeleteMedicalRecord, "Delete the uploaded Medical Record on the server");

                ToolTip tipDeleteUnknownDoc = new ToolTip();
                tipDeleteUnknownDoc.SetToolTip(btnDeleteUnknownDoc, "Delete the uploaded Unknown Document on the server");

                // Tooltips for Medical Bill tab

                // View buttons for Medical Bill tab
                ToolTip tipViewNPF = new ToolTip();
                tipViewNPF.SetToolTip(btnViewNPF, "View the Needs Processing Form on the server");

                ToolTip tipViewIB = new ToolTip();
                tipViewIB.SetToolTip(btnViewIB, "View the Itemized Bill on the server");

                ToolTip tipViewPoP = new ToolTip();
                tipViewPoP.SetToolTip(btnViewPoP, "View the Proof of Payment on the server");

                ToolTip tipViewMedRec = new ToolTip();
                tipViewMedRec.SetToolTip(btnViewMedRecord, "View the Medical Record on the server");

                ToolTip tipViewUnknownDoc = new ToolTip();
                tipViewUnknownDoc.SetToolTip(btnViewOtherDoc, "View the Unknown Documents on the server");

                // ToolTips for Illness button and Incident button
                ToolTip tipIllness = new ToolTip();
                tipIllness.SetToolTip(btnMedBill_lllness, "Create illness or choose illness");

                ToolTip tipIncident = new ToolTip();
                tipIncident.SetToolTip(btnMedBill_Incident, "Create incident or choose incident");

                // Tooltips on Settlement
                ToolTip tipAddNewSettlement = new ToolTip();
                tipAddNewSettlement.SetToolTip(btnAddNewSettlement, "Add New Settlement");

                //ToolTip tipEditSettlement = new ToolTip();
                //tipEditSettlement.SetToolTip(btnEditSettlement, "Edit the Seleted Settlement");

                ToolTip tipDeleteSettlement = new ToolTip();
                tipDeleteSettlement.SetToolTip(btnDeleteSettlement, "Delete the Selected Settlement");

                tbCMMManager.SelectedIndex = 1;

                PrevTabPage = TabPage.None;
                CurrentTabPage = TabPage.DashBoard;
            }

            //if (bLoginSuccess == true)
            //{

            //frmSearchResult searchResult = new frmSearchResult();

            // 09/28/18 begin here


            //    if (searchResult.ShowDialog() == DialogResult.OK)
            //    {
            //txtMembershipID.Text = searchResult.IndividualSelected.strMembershipID;
            //txtIndividualID.Text = searchResult.IndividualSelected.strIndividualID;

            //txtFirstName.Text = searchResult.IndividualSelected.strFirstName;
            //txtMiddleName.Text = searchResult.IndividualSelected.strMiddleName;
            //txtLastName.Text = searchResult.IndividualSelected.strLastName;
            //txtDateOfBirth.Text = searchResult.IndividualSelected.dtBirthDate.Value.ToString("MM/dd/yyyy");
            //cbGender.Items.Add("Male");
            //cbGender.Items.Add("Female");
            //if (searchResult.IndividualSelected.IndividualGender == Gender.Male) cbGender.SelectedIndex = 0;
            //else if (searchResult.IndividualSelected.IndividualGender == Gender.Female) cbGender.SelectedIndex = 1;
            //txtIndividualSSN.Text = searchResult.IndividualSelected.strSSN;

            //txtStreetAddress1.Text = searchResult.IndividualSelected.strShippingStreetAddress;
            //txtZip1.Text = searchResult.IndividualSelected.strShippingZip;
            //txtCity1.Text = searchResult.IndividualSelected.strShippingCity;
            //txtState1.Text = searchResult.IndividualSelected.strShippingState;

            //txtStreetAddress2.Text = searchResult.IndividualSelected.strBillingStreetAddress;
            //txtZip2.Text = searchResult.IndividualSelected.strBillingZip;
            //txtCity2.Text = searchResult.IndividualSelected.strBillingCity;
            //txtState2.Text = searchResult.IndividualSelected.strBillingState;
            //txtEmail.Text = searchResult.IndividualSelected.strEmail;

            //txtProgram.Text = searchResult.IndividualSelected.IndividualPlan.ToString();

            //switch (searchResult.IndividualSelected.IndividualPlan)
            //{
            //    case Plan.GoldPlus:
            //        txtMemberProgram.Text = "Gold Plus";
            //        break;
            //    case Plan.Gold:
            //        txtMemberProgram.Text = "Gold";
            //        break;
            //    case Plan.Silver:
            //        txtMemberProgram.Text = "Silver";
            //        break;
            //    case Plan.Bronze:
            //        txtMemberProgram.Text = "Bronze";
            //        break;
            //    case Plan.GoldMedi_I:
            //        txtMemberProgram.Text = "Gold Medi-I";
            //        break;
            //    case Plan.GoldMedi_II:
            //        txtMemberProgram.Text = "Gold Medi-II";
            //        break;
            //}

            //txtIndChurchName.Text = searchResult.IndividualSelected.strChurch;

            //txtMembershipStartDate.Text = searchResult.IndividualSelected.dtMembershipIndStartDate.Value.ToString("MM/dd/yyyy");
            //if (searchResult.IndividualSelected.dtMembershipCancelledDate != null)
            //{
            //    txtMembershipCancelledDate.Text = searchResult.IndividualSelected.dtMembershipCancelledDate.Value.ToString("MM/dd/yyyy");
            //}
            //else txtMembershipCancelledDate.Text = String.Empty;
            //txtIndMemberShipStatus.Text = searchResult.IndividualSelected.membershipStatus.ToString();

            //IndividualIdIndividualPage = txtIndividualID.Text.Trim();

            //String strSqlQueryForCaseInfo = "select distinct([dbo].[tbl_medbill].[Case_Id]), [dbo].[tbl_medbill].[Contact_Id], [dbo].[tbl_medbill].[BillStatus] " +
            //                                "from [dbo].[tbl_medbill] " +
            //                                "where [dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
            //                                "([dbo].[tbl_medbill].[BillStatus] = @BillStatusCode0 or " +
            //                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode1 or " +
            //                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode2 or " +
            //                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode3 or " +
            //                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode4)";

            //SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfo, connRN);
            //cmdQueryForCaseInfo.CommandType = CommandType.Text;

            //cmdQueryForCaseInfo.Parameters.AddWithValue("@IndividualId", IndividualIdIndividualPage);
            //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode0", 0);     // Pending
            //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode1", 1);     // Job Assigned to  
            //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode2", 2);     // In Progress
            //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode3", 3);     // Eligibility
            //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode4", 4);     // Under bill processing
            //                                                                        //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode5", 5);    

            //connRN.Open();
            //SqlDataReader rdrCaseInfo = cmdQueryForCaseInfo.ExecuteReader();

            //lstCaseInfo.Clear();
            //if (rdrCaseInfo.HasRows)
            //{
            //    while (rdrCaseInfo.Read())
            //    {
            //        lstCaseInfo.Add(new CaseInfo { CaseName = rdrCaseInfo.GetString(0), IndividualId = rdrCaseInfo.GetString(1) });
            //    }
            //}
            //connRN.Close();

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //if (lstCaseInfo.Count > 0)
            //{

            //    String strSqlQueryForCasesForIndividualID = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[CreateStaff], " +
            //                                                "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], [dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
            //                                                "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], [dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
            //                                                "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
            //                                                "from [dbo].[tbl_case] where [dbo].[tbl_case].[Contact_ID] = @IndividualID";


            //    SqlCommand cmdQueryForCasesIndividualPage = new SqlCommand(strSqlQueryForCasesForIndividualID, connRN);
            //    cmdQueryForCasesIndividualPage.CommandType = CommandType.Text;
            //    cmdQueryForCasesIndividualPage.Parameters.AddWithValue("@IndividualID", lstCaseInfo[0].IndividualId);

            //    cmdQueryForCasesIndividualPage.Notification = null;

            //    SqlDependency dependencyCaseForIndividual = new SqlDependency(cmdQueryForCasesIndividualPage);
            //    dependencyCaseForIndividual.OnChange += new OnChangeEventHandler(OnCaseForIndividualChange);

            //    connRN.Open();
            //    SqlDataReader rdrCasesForIndividual = cmdQueryForCasesIndividualPage.ExecuteReader();

            //    if (rdrCasesForIndividual.HasRows)
            //    {
            //        gvProcessingCaseNo.Rows.Clear();
            //        while (rdrCasesForIndividual.Read())
            //        {
            //            for (int i = 0; i < lstCaseInfo.Count; i++)
            //            {
            //                if ((!rdrCasesForIndividual.IsDBNull(0)) &&
            //                    (rdrCasesForIndividual.GetString(0) == lstCaseInfo[i].CaseName))
            //                {

            //                    DataGridViewRow row = new DataGridViewRow();

            //                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });

            //                    if (!rdrCasesForIndividual.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetString(0) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(1) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetInt16(2) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(3)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(3) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

            //                    if (!rdrCasesForIndividual.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(4) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(5)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(5) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

            //                    if (!rdrCasesForIndividual.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(6) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(7)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(7) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

            //                    if (!rdrCasesForIndividual.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(8) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(9) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(10) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(11)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(11) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    if (!rdrCasesForIndividual.IsDBNull(12)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(12) });
            //                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

            //                    gvProcessingCaseNo.Rows.Add(row);
            //                }
            //            }
            //        }
            //    }
            //    connRN.Close();
            //}

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// Case History Page

            //strIndividualId = searchResult.IndividualSelected.strIndividualID.Trim();

            //txtCaseHistoryIndividualID.Text = strIndividualId;

            //txtCaseHistoryIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text + " " + txtMiddleName.Text;

            //String strSqlQueryForCreateStaff = "select dbo.tbl_CreateStaff.CreateStaff_Id, dbo.tbl_CreateStaff.Staff_Name from dbo.tbl_CreateStaff";

            //SqlCommand cmdQueryForCreateStaff = new SqlCommand(strSqlQueryForCreateStaff, connRN);
            //cmdQueryForCreateStaff.CommandType = CommandType.Text;

            //connRN.Open();
            //SqlDataReader rdrCreateStaff = cmdQueryForCreateStaff.ExecuteReader();

            //lstCreateStaff.Clear();
            //if (rdrCreateStaff.HasRows)
            //{
            //    while (rdrCreateStaff.Read())
            //    {
            //        lstCreateStaff.Add(new StaffInfo { StaffId = rdrCreateStaff.GetInt16(0), StaffName = rdrCreateStaff.GetString(1) });
            //    }
            //}
            //connRN.Close();

            //String strSqlQueryForModifiStaff = "select dbo.tbl_ModifiStaff.ModifiStaff_Id, dbo.tbl_ModifiStaff.Staff_Name from dbo.tbl_ModifiStaff";

            //SqlCommand cmdQueryForModifiStaff = new SqlCommand(strSqlQueryForModifiStaff, connRN);
            //cmdQueryForModifiStaff.CommandType = CommandType.Text;

            //connRN.Open();
            //SqlDataReader rdrModifiStaff = cmdQueryForModifiStaff.ExecuteReader();

            //lstModifiStaff.Clear();
            //if (rdrModifiStaff.HasRows)
            //{
            //    while (rdrModifiStaff.Read())
            //    {
            //        lstModifiStaff.Add(new StaffInfo { StaffId = rdrModifiStaff.GetInt16(0), StaffName = rdrModifiStaff.GetString(1) });
            //    }
            //}
            //connRN.Close();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //String strSqlQueryForCases = "select dbo.tbl_case.Case_Name, dbo.tbl_case.CreateDate, dbo.tbl_case.CreateStaff, " +
            //                                "dbo.tbl_case.ModifiDate, dbo.tbl_case.ModifiStaff " +
            //                                "from dbo.tbl_case where individual_id = @IndividualId";


            //SqlCommand cmdQueryForCases = new SqlCommand(strSqlQueryForCases, connRN);
            //cmdQueryForCases.CommandType = CommandType.Text;

            //cmdQueryForCases.Parameters.AddWithValue("@IndividualId", strIndividualId);

            //SqlDependency dependencyCase = new SqlDependency(cmdQueryForCases);
            //dependencyCase.OnChange += new OnChangeEventHandler(OnCaseChange);


            //connRN.Open();
            //SqlDataReader reader = cmdQueryForCases.ExecuteReader();

            //if (reader.HasRows)
            //{
            //    gvCaseViewCaseHistory.Rows.Clear();
            //    while (reader.Read())
            //    {
            //        DataGridViewRow row = new DataGridViewRow();

            //        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
            //        row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(0) });     // Case ID

            //        // Create Date
            //        if (!reader.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(1).ToString("MM/dd/yyyy") });

            //        // Create Staff
            //        if (!reader.IsDBNull(2))
            //        {
            //            for (int i = 0; i < lstCreateStaff.Count; i++)
            //            {
            //                if (reader.GetInt16(2) == lstCreateStaff[i].StaffId)
            //                    row.Cells.Add(new DataGridViewTextBoxCell { Value = lstCreateStaff[i].StaffName });
            //            }
            //        }

            //        // Modifi Date
            //        if (!reader.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(3).ToString("MM/dd/yyyy") });

            //        // Modifi Staff
            //        if (!reader.IsDBNull(4))
            //        {
            //            for (int i = 0; i < lstModifiStaff.Count; i++)
            //            {
            //                if (reader.GetInt16(4) == lstModifiStaff[i].StaffId)
            //                    row.Cells.Add(new DataGridViewTextBoxCell { Value = lstModifiStaff[i].StaffName });
            //            }
            //        }
            //        gvCaseViewCaseHistory.Rows.Add(row);
            //    }
            //}
            //connRN.Close();

            //        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //        // Settlement DataGridView
            //        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //        ///
            //        /// Tooltips
            //        /// 

            //        //// Browse buttons for Case Creation tab
            //        //ToolTip tipBrowseForNPF = new ToolTip();
            //        //tipBrowseForNPF.SetToolTip(btnBrowseNPF, "Browse for Needs Processing Form");

            //        //ToolTip tipBrowseForIB = new ToolTip();
            //        //tipBrowseForIB.SetToolTip(btnBrowseIB, "Browse for Itemized Bill");

            //        //ToolTip tipBrowseForPoP = new ToolTip();
            //        //tipBrowseForPoP.SetToolTip(btnBrowsePoP, "Browse for Proof of Payment");

            //        //ToolTip tipBrowseForMedRcord = new ToolTip();
            //        //tipBrowseForMedRcord.SetToolTip(btnBrowseMR, "Browse for Medical Record");

            //        //ToolTip tipBrowseForUnknownDoc = new ToolTip();
            //        //tipBrowseForUnknownDoc.SetToolTip(btnBrowseUnknownDoc, "Browse for Unknown Document");

            //        //// Upload buttons for Case Creation tab
            //        //ToolTip tipUploadNPF = new ToolTip();
            //        //tipUploadNPF.SetToolTip(btnNPFFormUpload, "Upload the Needs Processing Form to the server");

            //        //ToolTip tipUploadIB = new ToolTip();
            //        //tipUploadIB.SetToolTip(btnIBUpload, "Upload the Itemized Bill to the server");

            //        //ToolTip tipUploadPoP = new ToolTip();
            //        //tipUploadPoP.SetToolTip(btnPoPUpload, "Upload the Proof of Payment to the server");

            //        //ToolTip tipUploadMedRec = new ToolTip();
            //        //tipUploadMedRec.SetToolTip(btnMedicalRecordUpload, "Upload the Medical Record to the server");

            //        //ToolTip tipUploadUnknownDoc = new ToolTip();
            //        //tipUploadUnknownDoc.SetToolTip(btnUnknownDocUpload, "Upload the Unknown Document to the server");

            //        //// View buttons for Case Creation tab
            //        //ToolTip tipViewNPFCreateCase = new ToolTip();
            //        //tipViewNPFCreateCase.SetToolTip(btnNPFFormView, "View the NPF Form on the server");

            //        //ToolTip tipViewIBCreateCase = new ToolTip();
            //        //tipViewIBCreateCase.SetToolTip(btnIBView, "View the Itemized Bill on the server");

            //        //ToolTip tipViewPoPCreateCase = new ToolTip();
            //        //tipViewPoPCreateCase.SetToolTip(btnPoPView, "View the Proof of Payment on the server");

            //        //ToolTip tipViewMedRecCreateCase = new ToolTip();
            //        //tipViewMedRecCreateCase.SetToolTip(btnMedicalRecordView, "View the Medical Record Document on the server");

            //        //ToolTip tipViewUnknownDocCreateCase = new ToolTip();
            //        //tipViewUnknownDocCreateCase.SetToolTip(btnOtherDocView, "View the Unknown Document on the server");


            //        //// Delete buttons for Case Creation tab
            //        //ToolTip tipDeleteNPF = new ToolTip();
            //        //tipDeleteNPF.SetToolTip(btnNPFFormDelete, "Delete the uploaded Needs Processing Form on the server");

            //        //ToolTip tipDeleteIB = new ToolTip();
            //        //tipDeleteIB.SetToolTip(btnDeleteIB, "Delete the uploaded Itemized Bill on the server");

            //        //ToolTip tipDeletePoP = new ToolTip();
            //        //tipDeletePoP.SetToolTip(btnDeletePoP, "Delete the uploaded Proof of Payment on the server");

            //        //ToolTip tipDeleteMedRec = new ToolTip();
            //        //tipDeleteMedRec.SetToolTip(btnDeleteMedicalRecord, "Delete the uploaded Medical Record on the server");

            //        //ToolTip tipDeleteUnknownDoc = new ToolTip();
            //        //tipDeleteUnknownDoc.SetToolTip(btnDeleteUnknownDoc, "Delete the uploaded Unknown Document on the server");

            //        //// Tooltips for Medical Bill tab

            //        //// View buttons for Medical Bill tab
            //        //ToolTip tipViewNPF = new ToolTip();
            //        //tipViewNPF.SetToolTip(btnViewNPF, "View the Needs Processing Form on the server");

            //        //ToolTip tipViewIB = new ToolTip();
            //        //tipViewIB.SetToolTip(btnViewIB, "View the Itemized Bill on the server");

            //        //ToolTip tipViewPoP = new ToolTip();
            //        //tipViewPoP.SetToolTip(btnViewPoP, "View the Proof of Payment on the server");

            //        //ToolTip tipViewMedRec = new ToolTip();
            //        //tipViewMedRec.SetToolTip(btnViewMedRecord, "View the Medical Record on the server");

            //        //ToolTip tipViewUnknownDoc = new ToolTip();
            //        //tipViewUnknownDoc.SetToolTip(btnViewOtherDoc, "View the Unknown Documents on the server");

            //        //// ToolTips for Illness button and Incident button
            //        //ToolTip tipIllness = new ToolTip();
            //        //tipIllness.SetToolTip(btnMedBill_lllness, "Create illness or choose illness");

            //        //ToolTip tipIncident = new ToolTip();
            //        //tipIncident.SetToolTip(btnMedBill_Incident, "Create incident or choose incident");

            //        //// Tooltips on Settlement
            //        //ToolTip tipAddNewSettlement = new ToolTip();
            //        //tipAddNewSettlement.SetToolTip(btnAddNewSettlement, "Add New Settlement");

            //        //ToolTip tipEditSettlement = new ToolTip();
            //        //tipEditSettlement.SetToolTip(btnEditSettlement, "Edit the Seleted Settlement");

            //        //ToolTip tipDeleteSettlement = new ToolTip();
            //        //tipDeleteSettlement.SetToolTip(btnDeleteSettlement, "Delete the Selected Settlement");


            //        tbCMMManager.SelectedIndex = 0;

            //        PrevTabPage = TabPage.None;
            //        CurrentTabPage = TabPage.DashBoard;
            //        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //    }
            //}
        }

        // Don't modify this method
        private void UpdateGridViewMedBillOnCase(String IndividualId)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //String strSqlQueryForCaseInfo = "select distinct([dbo].[tbl_medbill].[Case_Id]), [dbo].[tbl_medbill].[Contact_Id], [dbo].[tbl_medbill].[BillStatus] " +
            //                    "from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillStatus] = @BillStatus";

            //SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfo, connRN);
            //cmdQueryForCaseInfo.CommandType = CommandType.Text;

            String strSqlQueryForCaseInfo = "select distinct([dbo].[tbl_medbill].[Case_Id]), [dbo].[tbl_medbill].[Contact_Id], [dbo].[tbl_medbill].[BillStatus] " +
                                "from [dbo].[tbl_medbill] " +
                                "where [dbo].[tbl_medbill].[BillStatus] = @BillStatusCode0 or " +
                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode1 or " +
                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode2 or " +
                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode3 or " +
                                "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode4";
            //"[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode5";

            SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfo, connRN);
            cmdQueryForCaseInfo.CommandType = CommandType.Text;

            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode0", MedBillStatus.Pending);     // Pending
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode1", MedBillStatus.CMMPendingPayment);     // Job Assigned to  
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode2", MedBillStatus.Closed);     // In Progress
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode3", MedBillStatus.Ineligible);     // Eligibility
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode4", MedBillStatus.PartiallyIneligible);     // Under bill processing

            SqlDependency dependency = new SqlDependency(cmdQueryForCaseInfo);
            dependency.OnChange += new OnChangeEventHandler(OnMedBillOnCaseChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCaseInfo = cmdQueryForCaseInfo.ExecuteReader();
            lstCaseInfo.Clear();

            if (rdrCaseInfo.HasRows)
            {
                while (rdrCaseInfo.Read())
                {
                    lstCaseInfo.Add(new CaseInfo { CaseName = rdrCaseInfo.GetString(0), IndividualId = rdrCaseInfo.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();


            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            String strSqlQueryForCasesForIndividualID = "select distinct([dbo].[tbl_case].[Case_Name]), [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[CreateStaff], " +
                                                        "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], [dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                        "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], [dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                        "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
                                                        "from [dbo].[tbl_case] where [dbo].[tbl_case].[Contact_ID] = @IndividualID";

            SqlCommand cmdQueryForCasesIndividualPage = new SqlCommand(strSqlQueryForCasesForIndividualID, connRN);
            cmdQueryForCasesIndividualPage.CommandType = CommandType.Text;

            cmdQueryForCasesIndividualPage.Parameters.AddWithValue("@IndividualID", lstCaseInfo[0].IndividualId);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCasesForIndividual = cmdQueryForCasesIndividualPage.ExecuteReader();
            gvProcessingCaseNo.Rows.Clear();

            if (rdrCasesForIndividual.HasRows)
            {
                while (rdrCasesForIndividual.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    for (int i = 0; i < lstCaseInfo.Count; i++)
                    {
                        if ((!rdrCasesForIndividual.IsDBNull(0)) &&
                            (rdrCasesForIndividual.GetString(0) == lstCaseInfo[i].CaseName))
                        {

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetString(0) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(1) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetInt16(2) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(3)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(3) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(4) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(5)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(5) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(6) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(7)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(7) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(8) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(9) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(10) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            gvProcessingCaseNo.Rows.Add(row);


                        }
                    }
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

        }



        private void UpdateGridViewCaseHistory(String IndividualId)
        {

            String strSqlQueryForCases = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[CreateStaff], " +
                                         "[dbo].[tbl_case].[ModifiDate], [dbo].[tbl_case].[ModifiStaff] from [dbo].[tbl_case] " +
                                         "where [dbo].[tbl_case].[individual_id] = @IndividualId and " +
                                         "[dbo].[tbl_case].[IsDeleted] = 0 " +
                                         "order by [dbo].[tbl_case].[ID]";


            SqlCommand cmdQueryForCases = new SqlCommand(strSqlQueryForCases, connRN);
            cmdQueryForCases.CommandType = CommandType.Text;

            cmdQueryForCases.Parameters.AddWithValue("@IndividualId", strIndividualId);

            SqlDependency dependencyCase = new SqlDependency(cmdQueryForCases);
            dependencyCase.OnChange += new OnChangeEventHandler(OnCaseChange);

            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            ClearCaseInCaseViewSafely();

            SqlDataReader reader = cmdQueryForCases.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(0) });     // Case ID

                    // Create Date
                    if (!reader.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(1).ToString("MM/dd/yyyy") });

                    // Create Staff
                    if (!reader.IsDBNull(2))
                    {
                        for (int i = 0; i < lstCreateStaff.Count; i++)
                        {
                            if (reader.GetInt16(2) == lstCreateStaff[i].StaffId)
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = lstCreateStaff[i].StaffName });
                        }
                    }

                    // Modifi Date
                    if (!reader.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(3).ToString("MM/dd/yyyy") });

                    // Modifi Staff
                    if (!reader.IsDBNull(4))
                    {
                        for (int i = 0; i < lstModifiStaff.Count; i++)
                        {
                            if (reader.GetInt16(4) == lstModifiStaff[i].StaffId)
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = lstModifiStaff[i].StaffName });
                        }
                    }
                    AddRowToCaseInCaseViewSafely(row);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        private void UpdateGridViewCaseForIndividual()
        {

            String strSqlQueryForCaseInfo = "select distinct([dbo].[tbl_medbill].[Case_Id]), [dbo].[tbl_medbill].[Contact_Id], [dbo].[tbl_medbill].[BillStatus] " +
                                            "from [dbo].[tbl_medbill] " +
                                            "where [dbo].[tbl_medbill].[IsDeleted] = 0 and " +
                                            "([dbo].[tbl_medbill].[BillStatus] = @BillStatusCode0 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode1 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode2 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode3 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode4)";

            SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfo, connRN);
            cmdQueryForCaseInfo.CommandType = CommandType.Text;

            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode0", MedBillStatus.Pending);     // Pending
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode1", MedBillStatus.CMMPendingPayment);     // Job Assigned to  
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode2", MedBillStatus.Closed);     // In Progress
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode3", MedBillStatus.Ineligible);     // Eligibility
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode4", MedBillStatus.PartiallyIneligible);     // Under bill processing

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCaseInfo = cmdQueryForCaseInfo.ExecuteReader();
            lstCaseInfo.Clear();
            if (rdrCaseInfo.HasRows)
            {
                while (rdrCaseInfo.Read())
                {
                    lstCaseInfo.Add(new CaseInfo { CaseName = rdrCaseInfo.GetString(0), IndividualId = rdrCaseInfo.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            String strSqlQueryForCasesForIndividualID = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[CreateStaff], " +
                                            "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], [dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                            "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], [dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                            "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
                                            "from [dbo].[tbl_case] where [dbo].[tbl_case].[IsDeleted] = 0 and " +
                                            "[dbo].[tbl_case].[Contact_ID] = @IndividualID " +
                                            "order by [dbo].[tbl_case].[Case_Name]";

            SqlCommand cmdQueryForCasesIndividualPage = new SqlCommand(strSqlQueryForCasesForIndividualID, connRN);
            cmdQueryForCasesIndividualPage.CommandType = CommandType.Text;
            cmdQueryForCasesIndividualPage.Parameters.AddWithValue("@IndividualID", IndividualIdIndividualPage);

            cmdQueryForCasesIndividualPage.Notification = null;

            SqlDependency dependencyCaseForIndividual = new SqlDependency(cmdQueryForCasesIndividualPage);
            dependencyCaseForIndividual.OnChange += new OnChangeEventHandler(OnCaseForIndividualChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCasesForIndividual = cmdQueryForCasesIndividualPage.ExecuteReader();

            if (IsHandleCreated) ClearCaseInProcessSafely();
            else gvProcessingCaseNo.Rows.Clear();

            if (rdrCasesForIndividual.HasRows)
            {
                //gvProcessingCaseNo.Rows.Clear();
                while (rdrCasesForIndividual.Read())
                {
                    for (int i = 0; i < lstCaseInfo.Count; i++)
                    {
                        if ((!rdrCasesForIndividual.IsDBNull(0)) &&
                            (rdrCasesForIndividual.GetString(0) == lstCaseInfo[i].CaseName))
                        {
                            DataGridViewRow row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetString(0) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(1) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetInt16(2) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(3)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(3) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(4) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(5)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(5) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(6) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(7)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(7) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(8) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(9) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(10) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(11)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(11) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(12)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(12) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            //gvProcessingCaseNo.Rows.Add(row);
                            if (IsHandleCreated) AddRowToCaseInProcessSafely(row);
                            else gvProcessingCaseNo.Rows.Add(row);
                        }
                    }
                }
            }

            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        private void OnMedBillOnCaseChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnMedBillOnCaseChange;

                UpdateGridViewMedBillOnCase(strIndividualId);
            }
        }



        private void ClearCaseInProcessSafely()
        {
            //for (int i = 0; i < gvProcessingCaseNo.Rows.Count; i++)
            //{
            //    gvProcessingCaseNo.BeginInvoke(new RemoveCaseInProcess(RemoveRowCaseInProcess), 0);
            //}
            gvProcessingCaseNo.BeginInvoke(new RemoveAllCaseInProcess(RemoveAllCasesInProcess));
        }

        private void AddRowToCaseInProcessSafely(DataGridViewRow row)
        {
            gvProcessingCaseNo.BeginInvoke(new AddRowToCaseInProcess(AddRowCaseInProcess), row);
        }

        private void AddRowCaseInProcess(DataGridViewRow row)
        {
            gvProcessingCaseNo.Rows.Add(row);
        }

        private void RemoveAllCasesInProcess()
        {
            gvProcessingCaseNo.Rows.Clear();
        }

        private void RemoveRowCaseInProcess(int i)
        {
            gvProcessingCaseNo.Rows.RemoveAt(i);
        }

        private void OnCaseForIndividualChange(object sender, SqlNotificationEventArgs e)
        {

            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnCaseForIndividualChange;

                UpdateGridViewCaseForIndividual();
            }
        }

        private void ClearCaseInCaseViewSafely()
        {
            gvCaseViewCaseHistory.BeginInvoke(new RemoveAllCaseInCaseView(RemoveAllRowCaseInCaseView));
        }

        private void AddRowToCaseInCaseViewSafely(DataGridViewRow row)
        {
            gvCaseViewCaseHistory.BeginInvoke(new AddRowToCaseInCaseView(AddRowCaseInCaseView), row);
        }

        private void AddRowCaseInCaseView(DataGridViewRow row)
        {
            gvCaseViewCaseHistory.Rows.Add(row);
        }

        private void RemoveRowCaseInCaseView(int i)
        {
            gvCaseViewCaseHistory.Rows.RemoveAt(i);
        }

        private void RemoveAllRowCaseInCaseView()
        {
            gvCaseViewCaseHistory.Rows.Clear();
        }

        private void OnCaseChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnCaseChange;

                UpdateGridViewCaseHistory(strIndividualId);
            }
        }

        private void OnIllnessChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnIllnessChange;

                UpdateGridViewIllnessList(strIndividualId);
            }
        }

        private void OnIncidentChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnIncidentChange;

                UpdateGridViewIncidentList(strIndividualId);
            }
        }

        private void UpdateGridViewIllnessList(String individual_id)
        {
            String strSqlQueryForIllness = "select Individual_Id, Case_Id, ICD_10_Id, CreateDate, Body from dbo.tbl_illness " +
                                           "where Individual_Id = '" + individual_id + "'";

            SqlCommand cmdQueryForIllness = connRN.CreateCommand();
            cmdQueryForIllness.CommandType = CommandType.Text;
            cmdQueryForIllness.CommandText = strSqlQueryForIllness;

            SqlDependency dependency = new SqlDependency(cmdQueryForIllness);
            dependency.OnChange += new OnChangeEventHandler(OnIllnessChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader reader = cmdQueryForIllness.ExecuteReader();
            gvIllnessList.Rows.Clear();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(0) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(1) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(2) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(4) });

                    gvIllnessList.Rows.Add(row);

                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
        }


        private void UpdateGridViewIncidentList(String individual_id)
        {
            String strSqlQueryForIncident = "select dbo.tbl_incident.Individual_id, dbo.tbl_incident.Illness_id, dbo.tbl_incident.Case_id, " +
                                            "dbo.tbl_illness.ICD_10_Id, dbo.tbl_incident.CreateDate, dbo.tbl_incident.IncidentNote " +
                                            "from dbo.tbl_incident inner join dbo.tbl_illness on dbo.tbl_incident.Illness_id = dbo.tbl_illness.Illness_Id " +
                                            "where dbo.tbl_incident.Individual_Id = '" + individual_id + "'";

            SqlCommand cmdQueryForIncident = connRN.CreateCommand();
            cmdQueryForIncident.CommandType = CommandType.Text;
            cmdQueryForIncident.CommandText = strSqlQueryForIncident;

            SqlDependency dependency = new SqlDependency(cmdQueryForIncident);
            dependency.OnChange += new OnChangeEventHandler(OnIncidentChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader reader = cmdQueryForIncident.ExecuteReader();

            gvIncidentList.Rows.Clear();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetInt32(1) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(2) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(4) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(5) });

                    gvIncidentList.Rows.Add(row);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        //public void OnIllnessChange(object sender, SqlNotificationEventArgs e)
        //{
        //    if (e.Type == SqlNotificationType.Change)
        //    {
        //        String strSqlQueryForIllness = "select Individual_Id, Case_Id, ICD_10_Id, CreateDate, Body from tbl_illness where Individual_id = '" + strIndividualId + "'";

        //        SqlCommand cmdQueryForIllness = connIllness.CreateCommand();
        //        cmdQueryForIllness.CommandType = CommandType.Text;
        //        cmdQueryForIllness.CommandText = strSqlQueryForIllness;

        //        connIllness.Open();
        //        SqlDataReader rdrIllness = cmdQueryForIllness.ExecuteReader();

        //        if (rdrIllness.HasRows)
        //        {
        //            while (rdrIllness.Read())
        //            {
        //                DataGridViewRow row = new DataGridViewRow();
        //                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIllness.GetString(0) });
        //                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIllness.GetString(1) });
        //                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIllness.GetString(2) });
        //                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIllness.GetDateTime(3) });
        //                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIllness.GetString(4) });

        //                gvIllnessList.Rows.Add(row);
        //            }
        //        }
        //        connIllness.Close();
        //    }
        //}

        private void btnCreateNewCase_Click(object sender, EventArgs e)
        {

            caseMode = CaseMode.AddNew;

            String strSqlCaseCount = "select count(ID) from tbl_case";

            SqlCommand cmdCaseCount = connRN.CreateCommand();
            cmdCaseCount.CommandType = CommandType.Text;
            cmdCaseCount.CommandText = strSqlCaseCount;

            String strNewCaseName = String.Empty;
            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            Object objCaseCount = cmdCaseCount.ExecuteScalar();
            if (connRN.State == ConnectionState.Open) connRN.Close();

            //if ((Int32)cmdCaseCount.ExecuteScalar() == 0)
            if ((Int32)objCaseCount == 0)
            {
                strNewCaseName = "Case-1";
                String strCaseName = strNewCaseName;
                strCaseNameSelected = strNewCaseName;
                String strIndividualID = txtCaseHistoryIndividualID.Text.Trim();

                String strSqlQueryForMedBillsInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], " +
                                                      "[dbo].[tbl_CreateStaff].[Staff_Name], [dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                      "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], " +
                                                      "[dbo].[tbl_medbill].[Balance] " +
                                                      "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                      "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                      "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                      "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                      "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                      "[dbo].[tbl_medbill].[IsDeleted] = 0";

                SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillsInCase, connRN);
                cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

                cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", strCaseName);
                cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", strIndividualID);

                SqlDependency dependencyMedBillsInCase = new SqlDependency(cmdQueryForMedBillsInCase);
                dependencyMedBillsInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseViewChange);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                SqlDataReader rdrMedBillInCase = cmdQueryForMedBillsInCase.ExecuteReader();
                gvCasePageMedBills.Rows.Clear();
                if (rdrMedBillInCase.HasRows)
                {
                    while (rdrMedBillInCase.Read())
                    {
                        DataGridViewRow row = new DataGridViewRow();

                        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                        gvCasePageMedBills.Rows.Add(row);
                    }
                }
                if (connRN.State == ConnectionState.Open) connRN.Close();
            }
            else
            {
                String strSqlLastCaseId = "select max(ID) from tbl_case";

                SqlCommand cmdMaxCaseID = connRN.CreateCommand();
                cmdMaxCaseID.CommandType = CommandType.Text;
                cmdMaxCaseID.CommandText = strSqlLastCaseId;

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //Int32 nMaxId = (Int32)cmdMaxCaseID.ExecuteScalar();
                Object objMaxId = cmdMaxCaseID.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                Int32 nMaxId = 0;
                Int32 nResultMaxId = 0;
                if (objMaxId != null)
                {
                    if (Int32.TryParse(objMaxId.ToString(), NumberStyles.Integer, new CultureInfo("en-US"), out nResultMaxId)) nMaxId = nResultMaxId;
                }
                else
                {
                    MessageBox.Show("No case in case table", "Error", MessageBoxButtons.OK);
                    return;
                }

                String strSqlMaxCaseName = "select Case_Name from tbl_case where ID = " + nMaxId;

                SqlCommand cmdMaxCaseName = connRN.CreateCommand();
                cmdMaxCaseName.CommandType = CommandType.Text;
                cmdMaxCaseName.CommandText = strSqlMaxCaseName;

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String strMaxCaseName = (String)cmdMaxCaseName.ExecuteScalar();
                Object objMaxCaseName = cmdMaxCaseName.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String strMaxCaseName = String.Empty;

                if (objMaxCaseName != null)
                {
                    strMaxCaseName = objMaxCaseName.ToString();
                }
                else
                {
                    MessageBox.Show("No case name for Case Id: " + nMaxId, "Error", MessageBoxButtons.OK);
                    return;
                }

                Int32 nNewCaseNo = Int32.Parse(strMaxCaseName.Substring(5));
                nNewCaseNo++;
                strNewCaseName = "Case-" + nNewCaseNo.ToString();

                String strCaseName = strNewCaseName;
                strCaseNameSelected = strNewCaseName;
                String strIndividualID = txtCaseHistoryIndividualID.Text.Trim();

                String strSqlQueryForMedBillsInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], " +
                                                      "[dbo].[tbl_CreateStaff].[Staff_Name], [dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                      "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], " +
                                                      "[dbo].[tbl_medbill].[Balance] " +
                                                      "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                      "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                      "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                      "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                      "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                      "[dbo].[tbl_medbill].[IsDeleted] = 0";

                SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillsInCase, connRN);
                cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

                cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", strCaseName);
                cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", strIndividualID);

                SqlDependency dependencyMedBillsInCase = new SqlDependency(cmdQueryForMedBillsInCase);
                dependencyMedBillsInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseViewChange);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                SqlDataReader rdrMedBillInCase = cmdQueryForMedBillsInCase.ExecuteReader();
                gvCasePageMedBills.Rows.Clear();
                if (rdrMedBillInCase.HasRows)
                {
                    while (rdrMedBillInCase.Read())
                    {
                        DataGridViewRow row = new DataGridViewRow();

                        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                        gvCasePageMedBills.Rows.Add(row);
                    }
                }
                if (connRN.State == ConnectionState.Open) connRN.Close();

            }

            //connRN.Close();

            txtCaseName.Text = strNewCaseName;

            txtCaseIndividualID.Text = txtIndividualID.Text;

            if (txtMiddleName.Text == String.Empty) txtCreateCaseIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text;
            else txtCreateCaseIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text + " " + txtMiddleName.Text;

            chkNPF_CaseCreationPage.Checked = false;
            chkIB_CaseCreationPage.Checked = false;
            chkPoP_CaseCreationPage.Checked = false;
            chkMedicalRecordCaseCreationPage.Checked = false;
            chkOtherDocCaseCreationPage.Checked = false;

            txtNPFFormFilePath.Text = String.Empty;
            txtIBFilePath.Text = String.Empty;
            txtPopFilePath.Text = String.Empty;
            txtMedicalRecordFilePath.Text = String.Empty;
            txtOtherDocumentFilePath.Text = String.Empty;

            txtNPFUploadDate.Text = String.Empty;
            txtIBUploadDate.Text = String.Empty;
            txtPoPUploadDate.Text = String.Empty;
            txtMRUploadDate.Text = String.Empty;
            txtOtherDocUploadDate.Text = String.Empty;

            tbCMMManager.TabPages.Insert(4, tbpgCreateCase);
            tbCMMManager.SelectedIndex = 4;

            btnNewMedBill_Case.Enabled = false;
            btnEditMedBill.Enabled = false;
            btnDeleteMedBill.Enabled = false;

        }

        private void btnCaseCreationSaveUpper_Click(object sender, EventArgs e)
        {
            //if (caseMode == CaseMode.AddNew)
            //{
            //caseDetail.CaseId = txtCaseName.Text.Trim();

            //DateTime dtToday = DateTime.Now;
            //String strToday = DateTime.Now.ToString();

            String CaseName = txtCaseName.Text.Trim();
            String IndividualId = txtCaseIndividualID.Text.Trim();

            String strSqlQueryForCaseName = "select [dbo].[tbl_case].[Case_Name] from [dbo].[tbl_case] " +
                                            "where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualId";

            SqlCommand cmdQueryForCaseName = new SqlCommand(strSqlQueryForCaseName, connRN);
            cmdQueryForCaseName.CommandText = strSqlQueryForCaseName;
            cmdQueryForCaseName.CommandType = CommandType.Text;

            cmdQueryForCaseName.Parameters.AddWithValue("@CaseName", CaseName);
            cmdQueryForCaseName.Parameters.AddWithValue("@IndividualId", IndividualId);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            Object objCaseName = cmdQueryForCaseName.ExecuteScalar();
            if (connRN.State == ConnectionState.Open) connRN.Close();

            if (objCaseName == null)
            {
                frmSaveNewCase frmSaveNewCase = new frmSaveNewCase();
                frmSaveNewCase.StartPosition = FormStartPosition.CenterParent;

                DialogResult dlgResult = frmSaveNewCase.ShowDialog();

                if (dlgResult == DialogResult.Yes)
                {

                    String strCaseId = String.Empty;
                    String strIndividualID = String.Empty;
                    String strNPFormFilePath = String.Empty;
                    String strNPFUploadDate = String.Empty;
                    String strIBFilePath = String.Empty;
                    String strIBUploadDate = String.Empty;
                    String strPopFilePath = String.Empty;
                    String strPopUploadDate = String.Empty;
                    String strMedicalRecordFilePath = String.Empty;
                    String strMedicalRecordUploadDate = String.Empty;
                    String strUnknownDocumentFilePath = String.Empty;
                    String strUnknownDocUploadDate = String.Empty;
                    String strLogID = String.Empty;

                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = String.Empty;
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    caseDetail.ModificationDate = DateTime.Today;
                    caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;

                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;
                    
                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;

                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;

                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;

                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;

                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();
                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = false;
                    caseDetail.AddBill_Received_Date = null;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlCreateCase = "insert into tbl_case (IsDeleted, Case_Name, Contact_ID, CreateDate, ModifiDate, CreateStaff, ModifiStaff, Case_status, " +
                                               "NPF_Form, NPF_Form_File_Name, NPF_Form_Destination_File_Name, NPF_Receiv_Date, " +
                                               "IB_Form, IB_Form_File_Name, IB_Form_Destination_File_Name, IB_Receiv_Date, " +
                                               "POP_Form, POP_Form_File_Name, POP_Form_Destination_File_Name, POP_Receiv_Date, " +
                                               "MedRec_Form, MedRec_Form_File_Name, MedRec_Form_Destination_File_Name, MedRec_Receiv_Date, " +
                                               "Unknown_Form, Unknown_Form_File_Name, Unknown_Form_Destination_File_Name, Unknown_Receiv_Date, " +
                                               "Note, Log_ID, AddBill_Form, AddBill_receiv_Date, Remove_log, individual_id) " +
                                               "Values (@IsDeleted, @CaseId, @ContactId, @CreateDate, @ModifiDate, @CreateStaff, @ModifiStaff, @CaseStatus, " +
                                               "@NPF_Form, @NPF_Form_File_Name, @NPF_Form_Destination_File_Name, @NPF_Receive_Date, " +
                                               "@IB_Form, @IB_Form_File_Name, @IB_Form_Destination_File_Name, @IB_Receive_Date, " +
                                               "@POP_Form, @POP_Form_File_Name, @POP_Form_Destination_File_Name, @POP_Receive_Date, " +
                                               "@MedRecord_Form, @MedRecord_Form_File_Name, @MedRecord_Form_Destination_File_name, @MedRecord_Receive_Date, " +
                                               "@Unknown_Form, @Unknown_Form_File_Name, @Unknown_Form_Destination_File_Name, @Unknown_Receive_Date, " +
                                               "@Note, @Log_Id, @AddBill_Form, @AddBill_ReceiveDate, @Remove_Log, @Individual_Id)";

                    SqlCommand cmdInsertNewCase = new SqlCommand(strSqlCreateCase, connRN);
                    cmdInsertNewCase.CommandType = CommandType.Text;

                    cmdInsertNewCase.Parameters.AddWithValue("@IsDeleted", 0);
                    cmdInsertNewCase.Parameters.AddWithValue("@CaseId", caseDetail.CaseId);
                    cmdInsertNewCase.Parameters.AddWithValue("@ContactId", caseDetail.ContactId);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateDate", caseDetail.CreateDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateStaff", caseDetail.CreateStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@CaseStatus", caseDetail.Status);

                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", caseDetail.NPF_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", caseDetail.IB_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", caseDetail.POP_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form", caseDetail.MedicalRecord_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Note", caseDetail.Note);
                    cmdInsertNewCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdInsertNewCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", caseDetail.AddBill_Received_Date);
                    else cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", DBNull.Value);
                    if (caseDetail.Remove_Log == String.Empty) cmdInsertNewCase.Parameters.AddWithValue("@Remove_Log", DBNull.Value);
                    cmdInsertNewCase.Parameters.AddWithValue("@Individual_Id", caseDetail.Individual_Id);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nResult = cmdInsertNewCase.ExecuteNonQuery();

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nResult == 1)
                    {
                        MessageBox.Show("The case has been saved.", "Information");

                        caseDetail.CaseId = txtCaseName.Text.Trim();
                        strCaseIdSelected = caseDetail.CaseId;
                        strContactIdSelected = caseDetail.ContactId;

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }

                    return;
                }
                else if (dlgResult == DialogResult.Cancel)
                {
                    return;
                }
                //else if (dlgResult == DialogResult.No)
                //{
                //    //tbCMMManager.TabPages.Remove(tbpgCreateCase);
                //    //tbCMMManager.SelectedIndex = 3;

                //    return;
                //}

            }
            else if (objCaseName != null)    // Edit and update case
            {
                frmSaveChangeOnCase frmDlgSaveChange = new frmSaveChangeOnCase();

                frmDlgSaveChange.StartPosition = FormStartPosition.CenterParent;
                DialogResult dlgResult = frmDlgSaveChange.ShowDialog();

                //if (frmDlgSaveChange.DialogResult == DialogResult.Yes)
                if (dlgResult == DialogResult.Yes)
                {
                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = txtCaseName.Text.Trim();
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    //caseDetail.ModificationDate = DateTime.Today;
                    //caseDetail.CreateStaff = 8;     // WonJik
                    //caseDetail.ModifyingStaff = 8;  // WonJik
                    //caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;
                    //caseDetail.NPF_ReceivedDate = DateTime.Today;
                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;
                    //caseDetail.IB_ReceivedDate = DateTime.Today;
                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;
                    //caseDetail.POP_ReceivedDate = DateTime.Today;
                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;
                    //caseDetail.MedRec_ReceivedDate = DateTime.Today;
                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;
                    //caseDetail.Unknown_ReceivedDate = DateTime.Today;
                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;
                    //caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();

                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Note = txtNoteOnCase.Text.Trim();
                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = true;
                    caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlUpdateCase = "Update [dbo].[tbl_case] set [dbo].[tbl_case].[ModifiDate] = @ModifiDate, [dbo].[tbl_case].[ModifiStaff] = @ModifiStaff, " +
                                              "[dbo].[tbl_case].[NPF_Form] = @NPF_Form, [dbo].[tbl_case].[NPF_Form_File_Name] = @NPF_Form_File_Name, " +
                                              "[dbo].[tbl_case].[NPF_Form_Destination_File_Name] = @NPF_Form_Destination_File_Name, [dbo].[tbl_case].[NPF_Receiv_Date] = @NPF_Receiv_Date, " +
                                              "[dbo].[tbl_case].[IB_Form] = @IB_Form, [dbo].[tbl_case].[IB_Form_File_Name] = @IB_Form_File_Name, " +
                                              "[dbo].[tbl_case].[IB_Form_Destination_File_Name] = @IB_Form_Destination_File_Name, [dbo].[tbl_case].[IB_Receiv_Date] = @IB_Receiv_Date, " +
                                              "[dbo].[tbl_case].[POP_Form] = @POP_Form, [dbo].[tbl_case].[POP_Form_File_Name] = @POP_Form_File_Name, " +
                                              "[dbo].[tbl_case].[POP_Form_Destination_File_Name] = @POP_Form_Destination_File_Name, [dbo].[tbl_case].[POP_Receiv_Date] = @POP_Receiv_Date, " +
                                              "[dbo].[tbl_case].[MedRec_Form] = @MedRec_Form, [dbo].[tbl_case].[MedRec_Form_File_Name] = @MedRec_Form_File_Name, " +
                                              "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name] = @MedRec_Form_Destination_File_Name, [dbo].[tbl_case].[MedRec_Receiv_Date] = @MedRec_Receiv_Date, " +
                                              "[dbo].[tbl_case].[Unknown_Form] = @Unknown_Form, [dbo].[tbl_case].[Unknown_Form_File_Name] = @Unknown_Form_File_Name, " +
                                              "[dbo].[tbl_case].[Unknown_Form_Destination_File_Name] = @Unknown_Form_Destination_File_Name, [dbo].[tbl_case].[Unknown_Receiv_Date] = @Unknown_Receiv_Date, " +
                                              "[dbo].[tbl_case].[Note] = @CaseNote, [dbo].[tbl_case].[Log_ID] = @Log_Id, [dbo].[tbl_case].[AddBill_Form] = @AddBill_Form, " +
                                              "[dbo].[tbl_case].[AddBill_Receiv_Date] = @AddBill_Receiv_Date, [dbo].[tbl_case].[Remove_Log] = @Remove_Log " +
                                              "where [dbo].[tbl_case].[Case_Name] = @Case_Id";

                    SqlCommand cmdUpdateCase = new SqlCommand(strSqlUpdateCase, connRN);
                    cmdUpdateCase.CommandType = CommandType.Text;

                    cmdUpdateCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdUpdateCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", caseDetail.NPF_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", caseDetail.IB_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", caseDetail.POP_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form", caseDetail.MedicalRecord_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", DBNull.Value);
                   
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@CaseNote", caseDetail.Note);
                    cmdUpdateCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdUpdateCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", caseDetail.AddBill_Received_Date);
                    else cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@Remove_Log", caseDetail.Remove_Log);

                    cmdUpdateCase.Parameters.AddWithValue("@Case_Id", caseDetail.CaseId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowAffected = cmdUpdateCase.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowAffected == 1)
                    {
                        MessageBox.Show("The change has been saved.", "Information");

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }
                    else if (nRowAffected == 0) MessageBox.Show("Update failed", "Error");


                }
                else
                {
                    return;
                }
            }

            //btnNewMedBill_Case.Enabled = false;
            //btnEditMedBill.Enabled = false;
            //btnDeleteMedBill.Enabled = false;

            //tbCMMManager.TabPages.Remove(tbpgCreateCase);
            //tbCMMManager.SelectedIndex = 3;
        }

        private void btnNPFFormUpload_Click(object sender, EventArgs e)
        {

            //File.Copy(strNPFormFilePathSource, strNPFormFilePathDestination);
            try
            {
                File.Copy(txtNPFFormFilePath.Text.Trim(), strNPFormFilePathDestination);
                txtNPFUploadDate.Text = DateTime.Today.ToString("MM/dd/yyyy");
                chkNPF_CaseCreationPage.Checked = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message + "\n Please change the source file name.", "Error");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnNPFFormView.Enabled = true;
            btnNPFFormDelete.Enabled = true;
        }

        //private void btnBrowseNPTForm_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

        //    OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
        //    OpenSourceFileDlg.DefaultExt = "jpg";
        //    OpenSourceFileDlg.RestoreDirectory = true;

        //    if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
        //    {
        //        strNPFormFilePathSource = OpenSourceFileDlg.FileName;
        //        strNPFormFilePathDestination = strDestinationPath + OpenSourceFileDlg.SafeFileName;
        //        txtNPFFormFilePath.Text = strNPFormFilePathSource;
        //        return;
        //    }
        //    else return;
        //}

        private void btnNPFFormView_Click(object sender, EventArgs e)
        {
            if (strNPFormFilePathDestination != String.Empty)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = strNPFormFilePathDestination;
                Process.Start(processInfo);
            }
            else
            {
                MessageBox.Show("No NPF is uploaded");
            }
        }

        private void btnNPFFormDelete_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Are you sure to delete the NPF form?", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(strNPFormFilePathDestination);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }
                chkNPF_CaseCreationPage.Checked = false;
                txtNPFFormFilePath.Text = String.Empty;
                btnBrowseNPF.Enabled = false;
                btnNPFFormUpload.Enabled = false;
                txtNPFUploadDate.Text = String.Empty;
                btnNPFFormView.Enabled = false;
                btnNPFFormDelete.Enabled = false;

                MessageBox.Show("The NPF form has been deleted.", "Information");
            }
        }

        private void btnBrowseIB_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

            OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
            OpenSourceFileDlg.DefaultExt = "jpg";
            OpenSourceFileDlg.RestoreDirectory = true;

            if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
            {
                strIBFilePathSource = OpenSourceFileDlg.FileName;
                strIBFilePathDestination = strDestinationPath + "_IB_" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss") + "_" + OpenSourceFileDlg.SafeFileName;
                txtIBFilePath.Text = strIBFilePathSource;
                btnIBUpload.Enabled = true;
                return;
            }
            else return;
        }

        private void btnIBDateUpload_Click(object sender, EventArgs e)
        {
            try
            {
                File.Copy(txtIBFilePath.Text.Trim(), strIBFilePathDestination);
                txtIBUploadDate.Text = DateTime.Today.ToString("MM/dd/yyyy");
                chkIB_CaseCreationPage.Checked = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message + "\n Please change the source file name.", "Error");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro");
                return;
            }

            btnIBView.Enabled = true;
            btnDeleteIB.Enabled = true;
        }

        private void IBDateView_Click(object sender, EventArgs e)
        {
            if (strIBFilePathDestination != String.Empty)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = strIBFilePathDestination;
                Process.Start(processInfo);
            }
            else
            {
                MessageBox.Show("No IB is uploaded");
            }
        }

        private void btnIBDateDelete_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Are you sure to delete the IB?", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(strIBFilePathDestination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }

                chkIB_CaseCreationPage.Checked = false;
                txtIBFilePath.Text = String.Empty;
                btnBrowseIB.Enabled = false;
                btnIBUpload.Enabled = false;
                txtIBUploadDate.Text = String.Empty;
                btnIBView.Enabled = false;
                btnDeleteIB.Enabled = false;

                MessageBox.Show("The IB has been deleted.", "Information");
            }
        }

        private void btnBrowsePoP_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

            OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
            OpenSourceFileDlg.DefaultExt = "jpg";
            OpenSourceFileDlg.RestoreDirectory = true;

            if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
            {
                strPoPFilePathSource = OpenSourceFileDlg.FileName;
                strPopFilePathDestination = strDestinationPath + "_PoP_" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss") + "_" + OpenSourceFileDlg.SafeFileName;
                txtPopFilePath.Text = strPoPFilePathSource;
                btnPoPUpload.Enabled = true;
                return;
            }
            else return;
        }

        private void btnPoPDateUpload_Click(object sender, EventArgs e)
        {

            try
            {
                File.Copy(txtPopFilePath.Text.Trim(), strPopFilePathDestination);
                txtPoPUploadDate.Text = DateTime.Today.ToString("MM/dd/yyyy");
                chkPoP_CaseCreationPage.Checked = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message + "\n Please change the source file name.", "Error");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnPoPView.Enabled = true;
            btnDeletePoP.Enabled = true;
        }

        private void btnPoPDateView_Click(object sender, EventArgs e)
        {
            if (strPopFilePathDestination != String.Empty)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = strPopFilePathDestination;
                Process.Start(processInfo);
            }
            else
            {
                MessageBox.Show("No Pop is uploaded");
            }
        }

        private void btnPoPDateDelete_Click(object sender, EventArgs e)
        {

            DialogResult dlgResult = MessageBox.Show("Are you sure to delete the PoP?", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(strPopFilePathDestination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }

                chkPoP_CaseCreationPage.Checked = false;
                txtPopFilePath.Text = String.Empty;
                btnBrowsePoP.Enabled = false;
                btnPoPUpload.Enabled = false;
                txtPoPUploadDate.Text = String.Empty;
                btnPoPView.Enabled = false;
                btnDeletePoP.Enabled = false;

                MessageBox.Show("The PoP form has been deleted.", "Information");
            }
        }

        private void btnBrowseMR_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

            OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
            OpenSourceFileDlg.DefaultExt = "jpg";
            OpenSourceFileDlg.RestoreDirectory = true;

            if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
            {
                strMedRecordFilePathSource = OpenSourceFileDlg.FileName;
                strMedRecordFilePathDestination = strDestinationPath + "_MR_" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss") + "_" + OpenSourceFileDlg.SafeFileName;
                txtMedicalRecordFilePath.Text = strMedRecordFilePathSource;
                btnMedicalRecordUpload.Enabled = true;
                return;
            }
            else return;
        }

        private void btnMedicalRecordUpload_Click(object sender, EventArgs e)
        {
            try
            {
                File.Copy(txtMedicalRecordFilePath.Text.Trim(), strMedRecordFilePathDestination);
                txtMRUploadDate.Text = DateTime.Today.ToString("MM/dd/yyyy");
                chkMedicalRecordCaseCreationPage.Checked = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message + "\n Please change the source file name.", "Error");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnMedicalRecordView.Enabled = true;
            btnDeleteMedicalRecord.Enabled = true;

        }

        private void btnMedicalRecordView_Click(object sender, EventArgs e)
        {
            if (strMedRecordFilePathDestination != String.Empty)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = strMedRecordFilePathDestination;
                Process.Start(processInfo);
            }
            else
            {
                MessageBox.Show("No Medical Record is uploaded");
            }
        }

        private void btnMedicalRecordDelete_Click(object sender, EventArgs e)
        {

            DialogResult dlgResult = MessageBox.Show("Are you sure to delete Medical Record form?", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(strMedRecordFilePathDestination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }

                chkMedicalRecordCaseCreationPage.Checked = false;
                txtMedicalRecordFilePath.Text = String.Empty;
                btnBrowseMR.Enabled = false;
                btnMedicalRecordUpload.Enabled = false;
                txtMRUploadDate.Text = String.Empty;
                btnMedicalRecordView.Enabled = false;
                btnDeleteMedicalRecord.Enabled = false;

                MessageBox.Show("The Medical Record form has been deleted.", "Information");
            }
        }

        private void btnBrowseUnknownDoc_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

            OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
            OpenSourceFileDlg.DefaultExt = "jpg";
            OpenSourceFileDlg.RestoreDirectory = true;

            if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
            {
                strUnknownDocFilePathSource = OpenSourceFileDlg.FileName;
                strUnknownDocFilePathDestination = strDestinationPath + "_Unknown_" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss") + "_" + OpenSourceFileDlg.SafeFileName;
                txtOtherDocumentFilePath.Text = strUnknownDocFilePathSource;
                btnUnknownDocUpload.Enabled = true;
                return;
            }
            else return;
        }

        private void btnUnknownUpload_Click(object sender, EventArgs e)
        {
            try
            {
                File.Copy(txtOtherDocumentFilePath.Text.Trim(), strUnknownDocFilePathDestination);
                txtOtherDocUploadDate.Text = DateTime.Today.ToString("MM/dd/yyyy");
                chkOtherDocCaseCreationPage.Checked = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message + "\n Please change the source file name.", "Error");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnOtherDocView.Enabled = true;
            btnDeleteUnknownDoc.Enabled = true;
        }

        private void btnUnknownView_Click(object sender, EventArgs e)
        {
            if (strUnknownDocFilePathDestination != String.Empty)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = strUnknownDocFilePathDestination;
                Process.Start(processInfo);
            }
            else
            {
                MessageBox.Show("No Unknown doc is uploaded");
            }
        }

        private void btnUnknownDelete_Click(object sender, EventArgs e)
        {

            DialogResult dlgResult = MessageBox.Show("Are you sure to delete the Unknown form?", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(strUnknownDocFilePathDestination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }

                chkOtherDocCaseCreationPage.Checked = false;
                txtOtherDocumentFilePath.Text = String.Empty;
                btnBrowseUnknownDoc.Enabled = false;
                btnUnknownDocUpload.Enabled = false;
                txtOtherDocUploadDate.Text = String.Empty;
                btnOtherDocView.Enabled = false;
                btnDeleteUnknownDoc.Enabled = false;

                MessageBox.Show("The Unknown form has been deleted.", "Information");
            }
        }

        private void btnAddNewMedBill_Click(object sender, EventArgs e)
        {

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //
            // Medical Bill creation page
            //
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            String strSqlQueryForIndivdiualInfo = "select individual_id__c, name, birthdate, SOCIAL_SECURITY_NUMBER__C, MAILINGSTREET, MAILINGCITY, MAILINGSTATE, MAILINGPOSTALCODE " +
                                                "from dbo.contact where INDIVIDUAL_ID__C = @IndividualId";

            SqlCommand cmdQueryForIndividualInfo = new SqlCommand(strSqlQueryForIndivdiualInfo, connSalesforce);
            cmdQueryForIndividualInfo.Parameters.AddWithValue("@IndividualId", strIndividualId);

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            SqlDataReader rdrIndividualInSalesforce = cmdQueryForIndividualInfo.ExecuteReader();
            if (rdrIndividualInSalesforce.HasRows)
            {
                rdrIndividualInSalesforce.Read();
                txtIndividualIDMedBill.Text = rdrIndividualInSalesforce.GetString(0);
                txtPatientNameMedBill.Text = rdrIndividualInSalesforce.GetString(1);
                txtMedBillDOB.Text = rdrIndividualInSalesforce.GetDateTime(2).ToString("MM/dd/yyyy");
                txtMedBillSSN.Text = rdrIndividualInSalesforce.GetString(3);
                txtMedBillAddress.Text = rdrIndividualInSalesforce.GetString(4) + ", " + rdrIndividualInSalesforce.GetString(5) + ", " + rdrIndividualInSalesforce.GetString(6) + " " +
                                                 rdrIndividualInSalesforce.GetDouble(7).ToString();
            }

            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            String strSqlQueryForCaseId = "select distinct dbo.tbl_incident.Case_Id from dbo.tbl_incident where dbo.tbl_incident.Individual_id = @IndividualId";

            SqlCommand cmdQueryForCaseId = new SqlCommand(strSqlQueryForCaseId, connRN);
            cmdQueryForCaseId.Parameters.AddWithValue("@IndividualId", strIndividualId);

            cmdQueryForCaseId.CommandType = CommandType.Text;
            cmdQueryForCaseId.CommandText = strSqlQueryForCaseId;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrIncidentForCaseId = cmdQueryForCaseId.ExecuteReader();

            if (rdrIncidentForCaseId.HasRows)
            {
                while (rdrIncidentForCaseId.Read())
                {
                    txtMedBill_CaseNo.Text = rdrIncidentForCaseId.GetString(0);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            String strSqlQueryForDiseaseName = "select [dbo].[ICD10 Code].Name from [dbo].[ICD10 Code] where ICD10_CODE__C = @ICD10Code";

            SqlCommand cmdQueryForDiseaseName = new SqlCommand(strSqlQueryForDiseaseName, connSalesforce);
            cmdQueryForDiseaseName.Parameters.AddWithValue("@ICD10Code", txtMedBill_ICD10Code.Text.Trim());

            cmdQueryForDiseaseName.CommandType = CommandType.Text;
            cmdQueryForDiseaseName.CommandText = strSqlQueryForDiseaseName;

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
            SqlDataReader rdrDiseaseName = cmdQueryForDiseaseName.ExecuteReader();
            if (rdrDiseaseName.HasRows)
            {
                rdrDiseaseName.Read();
                txtMedBillDiseaseName.Text = rdrDiseaseName.GetString(0);
            }
            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            tbCMMManager.TabPages.Insert(4, tbpgMedicalBill);
            tbCMMManager.SelectedIndex = 4;

        }

        private void btnAddNewIllness_Click(object sender, EventArgs e)
        {
            frmIllnessCreationPage frmIllness = new frmIllnessCreationPage();

            frmIllness.txtIndividualNo.Text = txtIndividualID.Text;
            frmIllness.mode = IllnessMode.AddNew;

            frmIllness.ShowDialog(this);
        }

        private void gvCaseViewCaseHistory_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (tbCMMManager.TabPages.Contains(tbpgCreateCase))
            {
                MessageBox.Show("Case Page already open.");
            }
            else
            {
                DataGridView gvCaseHistory = (DataGridView)sender;

                int nRowSelected;

                strIndividualId = txtIndividualID.Text.Trim();

                //String strCaseNameSelected = String.Empty;
                String strPatientLastName = txtLastName.Text.Trim();
                String strPatientFirstName = txtFirstName.Text.Trim();
                String strPatientMiddleName = txtMiddleName.Text.Trim();
                String strDateOfBirth = dtpBirthDate.Value.ToString("MM/dd/yyyy");
                String strSSN = txtIndividualSSN.Text.Trim();
                String strStreetAddr = txtStreetAddress1.Text.Trim();
                String strCity = txtCity1.Text.Trim();
                String strState = txtState1.Text.Trim();
                String strZip = txtZip1.Text.Trim();

                if (gvCaseHistory.Rows.Count > 0)
                {
                    nRowSelected = e.RowIndex;

                    strCaseNameSelected = gvCaseHistory["CaseName", nRowSelected].Value.ToString();

                    String strSqlQueryForCase = "select [dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Form_Destination_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Form_Destination_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Form_Destination_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], " +
                                                "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], [dbo].[tbl_case].[Unknown_Form_Destination_File_Name], " +
                                                "[dbo].[tbl_case].[Unknown_Receiv_Date], [dbo].[tbl_case].[Case_status], [dbo].[tbl_case].[Note] " +
                                                "from [dbo].[tbl_case] " +
                                                "where [dbo].[tbl_case].[IsDeleted] = 0 and " +
                                                "[dbo].[tbl_case].[Case_Name] = @CaseName and " +
                                                "[dbo].[tbl_case].[Contact_ID] = @IndividualId";

                    SqlCommand cmdQueryForCase = new SqlCommand(strSqlQueryForCase, connRN);
                    cmdQueryForCase.CommandType = CommandType.Text;

                    cmdQueryForCase.Parameters.AddWithValue("@CaseName", strCaseNameSelected);
                    cmdQueryForCase.Parameters.AddWithValue("@IndividualId", strIndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrCase = cmdQueryForCase.ExecuteReader();
                    if (rdrCase.HasRows)
                    {
                        txtCaseName.Text = strCaseNameSelected;
                        txtCaseIndividualID.Text = strIndividualId;
                        txtCreateCaseIndividualName.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;

                        if (rdrCase.Read())
                        {
                            if (rdrCase.GetBoolean(0) == true) chkNPF_CaseCreationPage.Checked = true;
                            if (!rdrCase.IsDBNull(1)) txtNPFFormFilePath.Text = rdrCase.GetString(1);
                            if (!rdrCase.IsDBNull(2)) strNPFormFilePathDestination = rdrCase.GetString(2);
                            if (!rdrCase.IsDBNull(3)) txtNPFUploadDate.Text = rdrCase.GetDateTime(3).ToString("MM/dd/yyyy");
                            if (rdrCase.GetBoolean(4) == true) chkIB_CaseCreationPage.Checked = true;
                            if (!rdrCase.IsDBNull(5)) txtIBFilePath.Text = rdrCase.GetString(5);
                            if (!rdrCase.IsDBNull(6)) strIBFilePathDestination = rdrCase.GetString(6);
                            if (!rdrCase.IsDBNull(7)) txtIBUploadDate.Text = rdrCase.GetDateTime(7).ToString("MM/dd/yyyy");
                            if (rdrCase.GetBoolean(8) == true) chkPoP_CaseCreationPage.Checked = true;
                            if (!rdrCase.IsDBNull(9)) txtPopFilePath.Text = rdrCase.GetString(9);
                            if (!rdrCase.IsDBNull(10)) strPopFilePathDestination = rdrCase.GetString(10);
                            if (!rdrCase.IsDBNull(11)) txtPoPUploadDate.Text = rdrCase.GetDateTime(11).ToString("MM/dd/yyyy");
                            if (rdrCase.GetBoolean(12) == true) chkMedicalRecordCaseCreationPage.Checked = true;
                            if (!rdrCase.IsDBNull(13)) txtMedicalRecordFilePath.Text = rdrCase.GetString(13);
                            if (!rdrCase.IsDBNull(14)) strMedRecordFilePathDestination = rdrCase.GetString(14);
                            if (!rdrCase.IsDBNull(15)) txtMRUploadDate.Text = rdrCase.GetDateTime(15).ToString("MM/dd/yyyy");
                            if (rdrCase.GetBoolean(16) == true) chkOtherDocCaseCreationPage.Checked = true;
                            if (!rdrCase.IsDBNull(17)) txtOtherDocumentFilePath.Text = rdrCase.GetString(17);
                            if (!rdrCase.IsDBNull(18)) strUnknownDocFilePathDestination = rdrCase.GetString(18);
                            if (!rdrCase.IsDBNull(19)) txtOtherDocUploadDate.Text = rdrCase.GetDateTime(19).ToString("MM/dd/yyyy");
                            if (rdrCase.GetBoolean(20) == true) txtCaseStatus.Text = "Complete and Ready";
                            else txtCaseStatus.Text = "Pending - Additional Documents required";
                            if (!rdrCase.IsDBNull(21)) txtNoteOnCase.Text = rdrCase.GetString(21);
                        }

                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Med bills in Case Page

                    String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], " +
                                                         "[dbo].[tbl_CreateStaff].[Staff_Name], " +
                                                         "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                         "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], " +
                                                         "[dbo].[tbl_medbill].[Balance] " +
                                                         "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                         "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                         "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                         "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                         "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                         "[dbo].[tbl_medbill].[IsDeleted] = 0";

                    SqlCommand cmdQueryForMedBillInCase = new SqlCommand(strSqlQueryForMedBillInCase, connRN);
                    cmdQueryForMedBillInCase.CommandType = CommandType.Text;

                    cmdQueryForMedBillInCase.Parameters.AddWithValue("@CaseName", strCaseNameSelected);
                    cmdQueryForMedBillInCase.Parameters.AddWithValue("@IndividualId", strIndividualId);

                    SqlDependency dependencyMedBillsInCase = new SqlDependency(cmdQueryForMedBillInCase);
                    dependencyMedBillsInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseViewChange);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrMedBillInCase = cmdQueryForMedBillInCase.ExecuteReader();
                    gvCasePageMedBills.Rows.Clear();

                    if (rdrMedBillInCase.HasRows)
                    {
                        while (rdrMedBillInCase.Read())
                        {
                            DataGridViewRow row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                            gvCasePageMedBills.Rows.Add(row);
                            //AddNewRowToMedBillInCaseSafely(row);
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    btnNewMedBill_Case.Enabled = true;
                    btnEditMedBill.Enabled = true;
                    btnDeleteMedBill.Enabled = true;

                    tbCMMManager.TabPages.Insert(4, tbpgCreateCase);
                    tbCMMManager.SelectedIndex = 4;
                }
            }
        }

        private void OnMedBillsInCaseViewChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnMedBillsInCaseViewChange;

                UpdateGridViewMedBillsInCaseView();
            }
        }

        private void UpdateGridViewMedBillsInCaseView()
        {
            String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                                 "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                 "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                                                 "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                 "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                 "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                 "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                 "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and" +
                                                 "[dbo].[tbl_medbill].[IsDeleted] = 0";

            //String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], " +
            //                         "[dbo].[tbl_CreateStaff].[Staff_Name], " +
            //                         "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
            //                         "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], " +
            //                         "[dbo].[tbl_medbill].[Balance] " +
            //                         "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
            //                         "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
            //                         "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
            //                         "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
            //                         "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
            //                         "[dbo].[tbl_medbill].[IsDeleted] = 0";


            SqlCommand cmdQueryForMedBillInCase = new SqlCommand(strSqlQueryForMedBillInCase, connRN);
            cmdQueryForMedBillInCase.CommandType = CommandType.Text;

            cmdQueryForMedBillInCase.Parameters.AddWithValue("@CaseName", strCaseNameSelected);
            cmdQueryForMedBillInCase.Parameters.AddWithValue("@IndividualId", strIndividualId);

            SqlDependency dependencyMedBillsInCase = new SqlDependency(cmdQueryForMedBillInCase);
            dependencyMedBillsInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseViewChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrMedBillInCase = cmdQueryForMedBillInCase.ExecuteReader();

            if (IsHandleCreated) ClearMedBillInCaseSafely();
            else gvCasePageMedBills.Rows.Clear();

            if (rdrMedBillInCase.HasRows)
            {
                while (rdrMedBillInCase.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                    //gvCasePageMedBills.Rows.Add(row);
                    if (IsHandleCreated) AddNewRowToMedBillInCaseSafely(row);
                    else gvCasePageMedBills.Rows.Add(row);
                }
            }

            if (connRN.State == ConnectionState.Open) connRN.Close();
        }


        //private void UpdateGridViewMedBillsInCase()
        //{

        //}

        private void btnEditIllness_Click(object sender, EventArgs e)
        {
            //String strCaseName = cbCurrentCase.SelectedItem.ToString();

            //DataGridViewRow row = gvIllnessList.SelectedRows[0];

            //String strIndividualNo = row.Cells[0].Value.ToString();
            //String strICD10Code = row.Cells[1].Value.ToString();

            //String strConnStringIllnessEdit = @"Data Source=CMM-2014U\CMM; Initial Catalog=RN_DB;Integrated Security=True";

            //SqlConnection illnessEditConn = new SqlConnection(strConnStringIllnessEdit);
            //SqlCommand cmdIllnessEdit = illnessEditConn.CreateCommand();

            //String strQueryForIllness = "select Individual_Id, ICD_10_Id, Date_of_Diagnosis, CreateDate, Introduction, Body, Conclusion from tbl_illness " +
            //                            "where Individual_Id = '" + strIndividualNo + "' and Case_Id = '" + strCaseName + "' and ICD_10_Id = '" + strICD10Code + "'";

            //cmdIllnessEdit.CommandType = CommandType.Text;
            //cmdIllnessEdit.CommandText = strQueryForIllness;

            //illnessEditConn.Open();

            //SqlDataReader rdrIllness = cmdIllnessEdit.ExecuteReader();

            //if (rdrIllness.HasRows)
            //{
            //    frmIllnessCreationPage frm = new frmIllnessCreationPage();

            //    while(rdrIllness.Read())
            //    {
            //        frm.mode = IllnessMode.Edit;
            //        frm.strCaseId = cbCurrentCase.SelectedItem.ToString();
            //        frm.txtIndividualNo.Text = rdrIllness.GetString(0);
            //        frm.txtICD10Code.Text = rdrIllness.GetString(1);
            //        frm.dtpDateOfDiagnosis.Value = rdrIllness.GetDateTime(2);
            //        frm.dtpCreateDate.Value = rdrIllness.GetDateTime(3);
            //        frm.txtIntroduction.Text = rdrIllness.GetString(4);
            //        frm.txtIllnessNote.Text = rdrIllness.GetString(5);
            //        frm.txtConclusion.Text = rdrIllness.GetString(6);

            //        String strDiseaseNameConnString = @"Data Source=CMM-2014U\CMM; Initial Catalog=SalesForce;Integrated Security=True";

            //        SqlConnection connDiseaseName = new SqlConnection(strDiseaseNameConnString);
            //        SqlCommand cmdDiseaseName = connDiseaseName.CreateCommand();

            //        String strQueryForDiseaseName = "select Name from [ICD10 Code] where ICD10_CODE__C = '" + frm.txtICD10Code.Text.Trim() + "'";
            //        cmdDiseaseName.CommandType = CommandType.Text;
            //        cmdDiseaseName.CommandText = strQueryForDiseaseName;

            //        connDiseaseName.Open();
            //        frm.txtDiseaseName.Text = cmdDiseaseName.ExecuteScalar().ToString();
            //        connDiseaseName.Close();

            //    }

            //    frm.ShowDialog();
            //}


            //String strCreateDate = row.Cells[2].Value.ToString();
            //String strIllnessNote = row.Cells[3].Value.ToString();

            //frmIllnessCreationPage frmIllness = new frmIllnessCreationPage();

            //frmIllness.txtIndividualNo.Text = strIndividualNo;
            //frmIllness.txtICD10Code.Text = strICD10Code;
            //frmIllness.txtIllnessNote.Text = strIllnessNote;

            //frmIllness.ShowDialog();
        }

        private void btnDeleteIllness_Click(object sender, EventArgs e)
        {

            frmIllnessDeleteConfirm frm = new frmIllnessDeleteConfirm();

            //if (frm.ShowDialog() == DialogResult.Yes)
            //{
            //    String strCaseName = cbCurrentCase.SelectedItem.ToString();
            //    DataGridViewRow row = gvIllnessList.SelectedRows[0];
            //    String strIndividualNo = row.Cells[0].Value.ToString();

            //    String strSqlDeleteIllness = "delete from tbl_illness where Individual_Id = '" + strIndividualNo + "' and Case_Id = '" + strCaseName + "'";
            //    String strDeleteIllnessConnString = @"Data Source=CMM-2014U\CMM; Initial Catalog=RN_DB;Integrated Security=True";
            //    SqlConnection connDeleteIllness = new SqlConnection(strDeleteIllnessConnString);

            //    SqlCommand cmdDeleteIllness = connDeleteIllness.CreateCommand();
            //    cmdDeleteIllness.CommandType = CommandType.Text;
            //    cmdDeleteIllness.CommandText = strSqlDeleteIllness;

            //    connDeleteIllness.Open();
            //    if (cmdDeleteIllness.ExecuteNonQuery() == 1)
            //    {
            //        frmDeleteSuccess frmSuccess = new frmDeleteSuccess();
            //        frmSuccess.ShowDialog();
            //        return;
            //    }
            //    connDeleteIllness.Close();
            //}
            //else return;
        }

        private void frmCMMManager_FormClosing(object sender, FormClosingEventArgs e)
        {

            //SqlDependency.Stop(connRN_str);
            //SqlDependency.Stop(strConnStringForIllness);


        }


        //void txtMedBillGuarantor_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedBill_Illness_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedBill_Incident_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedBillAmount_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtBalance_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtPrescriptionName_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtPrescriptionNo_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtPrescriptionDescription_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtNumPhysicalTherapy_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void cbMedicalBillNote1_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void cbMedicalBillNote2_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void cbMedicalBillNote3_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void cbMedicalBillNote4_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedicalBillNote1_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedicalBillNote2_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedicalBillNote3_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void txtMedicalBillNote4_TextChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void dtpBillDate_ValueChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //void dtpDueDate_ValueChanged(object sender, EventArgs e)
        //{
        //    bIsModified = true;
        //}

        //private void OnCaseChanged(object sender, SqlDependencyEx.TableChangedEventArgs e)
        //{
        //    //Do stuff from e.Data
        //    MessageBox.Show("Case Table Changed!");
        //}

        private void btnAddNewIncident_Click(object sender, EventArgs e)
        {
            frmIncidentCreationPage frmIncident = new frmIncidentCreationPage();
            frmIncident.strIndividualId = txtIndividualID.Text.Trim();

            frmIncident.ShowDialog(this);
        }

        //private void cbBillCreationCaseNo_SelectedValueChanged(object sender, EventArgs e)
        //{
        //    ComboBox cbCaseNo = sender as ComboBox;

        //    String strCaseIdSelected = cbCaseNo.SelectedItem.ToString();

        //    String strSqlQueryForIncidentId = "select dbo.tbl_incident.incident_id from dbo.tbl_incident where dbo.tbl_incident.Case_id = @CaseId and dbo.tbl_incident.Individual_id = @IndividualId";

        //    SqlCommand cmdQueryForIncidentId = new SqlCommand(strSqlQueryForIncidentId, connRN);
        //    cmdQueryForIncidentId.Parameters.AddWithValue("@CaseId", strCaseIdSelected);
        //    cmdQueryForIncidentId.Parameters.AddWithValue("@IndividualId", strIndividualId);

        //    cmdQueryForIncidentId.CommandType = CommandType.Text;
        //    cmdQueryForIncidentId.CommandText = strSqlQueryForIncidentId;

        //    if (connRN.State == ConnectionState.Open) connRN.Close();

        //    connRN.Open();

        //    SqlDataReader rdrIncidentForIncidentId = cmdQueryForIncidentId.ExecuteReader();
        //    if (rdrIncidentForIncidentId.HasRows)
        //    {
        //        cbBillCreationIncidentNo.Items.Clear();
        //        while (rdrIncidentForIncidentId.Read())
        //        {
        //            cbBillCreationIncidentNo.Items.Add(rdrIncidentForIncidentId.GetInt32(0).ToString());
        //        }
        //        cbBillCreationIncidentNo.SelectedIndex = 0;
        //    }
        //    connRN.Close();


        //    String strQueryForDocReceivedDate = "select NPF_Receiv_Date, IB_Receiv_Date, POP_Receiv_Date from dbo.tbl_case where Case_Name = @CaseId";

        //    //String strCaseNo = cbBillCreationCaseNo.SelectedItem.ToString();
        //    String strCaseNo = txtMedBillCreationCaseNo.Text.Trim();

        //    SqlCommand cmdQueryForDocReceivDates = new SqlCommand(strQueryForDocReceivedDate, connRN);
        //    cmdQueryForDocReceivDates.CommandType = CommandType.Text;
        //    cmdQueryForDocReceivDates.CommandText = strQueryForDocReceivedDate;

        //    cmdQueryForDocReceivDates.Parameters.AddWithValue("@CaseId", strCaseNo);

        //    connRN.Open();
        //    SqlDataReader rdrDocReceivDate = cmdQueryForDocReceivDates.ExecuteReader();

        //    if (rdrDocReceivDate.HasRows)
        //    {
        //        rdrDocReceivDate.Read();
        //        txtNPFReceivedDate.Text = rdrDocReceivDate.GetDateTime(0).ToString("MM/dd/yyyy");
        //        txtIBReceivedDate.Text = rdrDocReceivDate.GetDateTime(1).ToString("MM/dd/yyyy");
        //        txtPOPReceivedDate.Text = rdrDocReceivDate.GetDateTime(2).ToString("MM/dd/yyyy");
        //    }

        //    connRN.Close();

        //}

        //private void cbBillCreationIncidentNo_SelectedValueChanged(object sender, EventArgs e)
        //{
        //    ComboBox cbIncidentNo = sender as ComboBox;

        //    String strIncidentNoSelected = cbIncidentNo.SelectedItem.ToString();

        //    String strSqlQueryForIllnessId = "select dbo.tbl_incident.illness_id from dbo.tbl_incident where dbo.tbl_incident.incident_id = @IncidentNo";

        //    SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
        //    cmdQueryForIllnessId.Parameters.AddWithValue("@IncidentNo", strIncidentNoSelected);

        //    cmdQueryForIllnessId.CommandType = CommandType.Text;
        //    cmdQueryForIllnessId.CommandText = strSqlQueryForIllnessId;

        //    if (connRN.State == ConnectionState.Open) connRN.Close();

        //    //String strIllnessId = String.Empty;
        //    Int32 nIllnessId = 0;
        //    rn_cnn.Open();

        //    SqlDataReader rdrIllnessId = cmdQueryForIllnessId.ExecuteReader();
        //    if (rdrIllnessId.HasRows)
        //    {
        //        rdrIllnessId.Read();
        //        nIllnessId = rdrIllnessId.GetInt32(0);
        //    }
        //    rn_cnn.Close();

        //    String strSqlQueryForICD10Code = "select dbo.tbl_illness.ICD_10_Id from dbo.tbl_illness where dbo.tbl_illness.Illness_id = @IllnessId";

        //    SqlCommand cmdQueryForICD10Code = new SqlCommand(strSqlQueryForICD10Code, rn_cnn);
        //    cmdQueryForICD10Code.Parameters.AddWithValue("@IllnessId", nIllnessId);

        //    cmdQueryForICD10Code.CommandType = CommandType.Text;
        //    cmdQueryForICD10Code.CommandText = strSqlQueryForICD10Code;

        //    if (rn_cnn.State == ConnectionState.Open) rn_cnn.Close();

        //    String strICD10Code = String.Empty;
        //    rn_cnn.Open();

        //    SqlDataReader rdrICD10Code = cmdQueryForICD10Code.ExecuteReader();
        //    if (rdrICD10Code.HasRows)
        //    {
        //        rdrICD10Code.Read();
        //        strICD10Code = rdrICD10Code.GetString(0);
        //    }
        //    rn_cnn.Close();

        //    txtICD10Code.Text = strICD10Code.Trim();

        //    String strSqlQueryForDiseaseName = "select dbo.[ICD10 Code].Name from dbo.[ICD10 Code] where ICD10_CODE__C = @ICD10Code";

        //    SqlCommand cmdQueryForDiseaseName = new SqlCommand(strSqlQueryForDiseaseName, connSalesforce);
        //    cmdQueryForDiseaseName.Parameters.AddWithValue("@ICD10Code", txtICD10Code.Text.Trim());

        //    cmdQueryForDiseaseName.CommandType = CommandType.Text;
        //    cmdQueryForDiseaseName.CommandText = strSqlQueryForDiseaseName;

        //    connSalesforce.Open();

        //    SqlDataReader rdrDiseaseName = cmdQueryForDiseaseName.ExecuteReader();
        //    if (rdrDiseaseName.HasRows)
        //    {
        //        rdrDiseaseName.Read();
        //        txtDiseaseName.Text = rdrDiseaseName.GetString(0);
        //    }
        //    connSalesforce.Close();

        //}

        private void txtICD10Code_TextChanged(object sender, EventArgs e)
        {
            String strICD10Code = txtMedBill_ICD10Code.Text.Trim();

            for (int i = 0; i < lstICD10CodeInfo.Count; i++)
            {
                if (strICD10Code.ToUpper() == lstICD10CodeInfo[i].ICD10Code)
                    txtMedBillDiseaseName.Text = lstICD10CodeInfo[i].Name;
            }
        }

        private void btnNewIncident_Click(object sender, EventArgs e)
        {
            frmIncidentCreationPage frmIncident = new frmIncidentCreationPage();
            frmIncident.strIndividualId = txtIndividualID.Text.Trim();

            frmIncident.ShowDialog(this);
        }

        private void btnNewIllness_Click(object sender, EventArgs e)
        {
            frmIllnessCreationPage frmIllness = new frmIllnessCreationPage();

            frmIllness.txtIndividualNo.Text = txtIndividualID.Text;
            frmIllness.mode = IllnessMode.AddNew;

            frmIllness.ShowDialog(this);
        }

        private void btnCaseCreationCancelUpper_Click(object sender, EventArgs e)
        {
            if (tbCMMManager.Contains(tbpgMedicalBill))
            {
                MessageBox.Show("Medical Bill page is open. Close Medical Bill page first.", "Alert");
                return;
            }

            DialogResult dlgClose = MessageBox.Show("Do you want to close Case page?", "Alert", MessageBoxButtons.YesNo);

            if (dlgClose == DialogResult.Yes)
            {
                DialogResult dlgResult = MessageBox.Show("Do you want save the change?", "Alert", MessageBoxButtons.YesNoCancel);

                if (dlgResult == DialogResult.Yes)
                {
                    String CaseName = txtCaseName.Text.Trim();
                    String IndividualId = txtCaseIndividualID.Text.Trim();

                    String strSqlQueryForCaseName = "select [dbo].[tbl_case].[Case_Name] from [dbo].[tbl_case] " +
                                                    "where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualId";

                    SqlCommand cmdQueryForCaseName = new SqlCommand(strSqlQueryForCaseName, connRN);
                    cmdQueryForCaseName.CommandText = strSqlQueryForCaseName;
                    cmdQueryForCaseName.CommandType = CommandType.Text;

                    cmdQueryForCaseName.Parameters.AddWithValue("@CaseName", CaseName);
                    cmdQueryForCaseName.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    Object objCaseName = cmdQueryForCaseName.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (objCaseName == null)
                    {
                        //frmSaveNewCase frmSaveNewCase = new frmSaveNewCase();

                        //DialogResult dlgResult = frmSaveNewCase.ShowDialog();

                        //if (dlgResult == DialogResult.Yes)
                        //{

                        String strCaseId = String.Empty;
                        String strIndividualID = String.Empty;
                        String strNPFormFilePath = String.Empty;
                        String strNPFUploadDate = String.Empty;
                        String strIBFilePath = String.Empty;
                        String strIBUploadDate = String.Empty;
                        String strPopFilePath = String.Empty;
                        String strPopUploadDate = String.Empty;
                        String strMedicalRecordFilePath = String.Empty;
                        String strMedicalRecordUploadDate = String.Empty;
                        String strUnknownDocumentFilePath = String.Empty;
                        String strUnknownDocUploadDate = String.Empty;
                        String strLogID = String.Empty;

                        CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                        caseDetail.CaseId = String.Empty;
                        caseDetail.ContactId = String.Empty;
                        caseDetail.Individual_Id = String.Empty;
                        caseDetail.CreateDate = DateTime.Today;
                        caseDetail.ModificationDate = DateTime.Today;
                        caseDetail.CreateStaff = nLoggedUserId;
                        caseDetail.ModifyingStaff = nLoggedUserId;
                        caseDetail.Status = false;
                        caseDetail.Individual_Id = String.Empty;
                        caseDetail.NPF_Form = 0;
                        caseDetail.NPF_Form_File_Name = String.Empty;
                        caseDetail.NPF_Form_Destination_File_Name = String.Empty;

                        caseDetail.IB_Form = 0;
                        caseDetail.IB_Form_File_Name = String.Empty;
                        caseDetail.IB_Form_Destination_File_Name = String.Empty;

                        caseDetail.POP_Form = 0;
                        caseDetail.POP_Form_File_Name = String.Empty;
                        caseDetail.POP_Form_Destionation_File_Name = String.Empty;

                        caseDetail.MedicalRecord_Form = 0;
                        caseDetail.MedRec_Form_File_Name = String.Empty;
                        caseDetail.MedRec_Form_Destination_File_Name = String.Empty;

                        caseDetail.Unknown_Form = 0;
                        caseDetail.Unknown_Form_File_Name = String.Empty;
                        caseDetail.Unknown_Form_Destination_File_Name = String.Empty;

                        caseDetail.Note = String.Empty;
                        caseDetail.Log_Id = String.Empty;
                        caseDetail.AddBill_Form = false;

                        caseDetail.Remove_Log = String.Empty;

                        if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                        if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                        if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();
                        if (chkNPF_CaseCreationPage.Checked)
                        {
                            caseDetail.NPF_Form = 1;
                            if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                            if (txtNPFUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                        }
                        if (chkIB_CaseCreationPage.Checked)
                        {
                            caseDetail.IB_Form = 1;
                            if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                            if (txtIBUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                        }
                        if (chkPoP_CaseCreationPage.Checked)
                        {
                            caseDetail.POP_Form = 1;
                            if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                            if (txtPoPUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                        }
                        if (chkMedicalRecordCaseCreationPage.Checked)
                        {
                            caseDetail.MedicalRecord_Form = 1;
                            if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                            if (txtMRUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                        }
                        if (chkOtherDocCaseCreationPage.Checked)
                        {
                            caseDetail.Unknown_Form = 1;
                            if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                            if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                        }

                        caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                        caseDetail.AddBill_Form = false;
                        caseDetail.AddBill_Received_Date = null;
                        caseDetail.Remove_Log = String.Empty;

                        String strSqlCreateCase = "insert into tbl_case (Case_Name, Contact_ID, CreateDate, ModifiDate, CreateStaff, ModifiStaff, Case_status, " +
                                                    "NPF_Form, NPF_Form_File_Name, NPF_Form_Destination_File_Name, NPF_Receiv_Date, " +
                                                    "IB_Form, IB_Form_File_Name, IB_Form_Destination_File_Name, IB_Receiv_Date, " +
                                                    "POP_Form, POP_Form_File_Name, POP_Form_Destination_File_Name, POP_Receiv_Date, " +
                                                    "MedRec_Form, MedRec_Form_File_Name, MedRec_Form_Destination_File_Name, MedRec_Receiv_Date, " +
                                                    "Unknown_Form, Unknown_Form_File_Name, Unknown_Form_Destination_File_Name, Unknown_Receiv_Date, " +
                                                    "Note, Log_ID, AddBill_Form, AddBill_receiv_Date, Remove_log, individual_id) " +
                                                    "Values (@CaseId, @ContactId, @CreateDate, @ModifiDate, @CreateStaff, @ModifiStaff, @CaseStatus, " +
                                                    "@NPF_Form, @NPF_Form_File_Name, @NPF_Form_Destination_File_Name, @NPF_Receive_Date, " +
                                                    "@IB_Form, @IB_Form_File_Name, @IB_Form_Destination_File_Name, @IB_Receive_Date, " +
                                                    "@POP_Form, @POP_Form_File_Name, @POP_Form_Destination_File_Name, @POP_Receive_Date, " +
                                                    "@MedRecord_Form, @MedRecord_Form_File_Name, @MedRecord_Form_Destination_File_name, @MedRecord_Receive_Date, " +
                                                    "@Unknown_Form, @Unknown_Form_File_Name, @Unknown_Form_Destination_File_Name, @Unknown_Receive_Date, " +
                                                    "@Note, @Log_Id, @AddBill_Form, @AddBill_ReceiveDate, @Remove_Log, @Individual_Id)";

                        SqlCommand cmdInsertNewCase = new SqlCommand(strSqlCreateCase, connRN);
                        cmdInsertNewCase.CommandType = CommandType.Text;

                        cmdInsertNewCase.Parameters.AddWithValue("@CaseId", caseDetail.CaseId);
                        cmdInsertNewCase.Parameters.AddWithValue("@ContactId", caseDetail.ContactId);
                        cmdInsertNewCase.Parameters.AddWithValue("@CreateDate", caseDetail.CreateDate);
                        cmdInsertNewCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                        cmdInsertNewCase.Parameters.AddWithValue("@CreateStaff", caseDetail.CreateStaff);
                        cmdInsertNewCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                        cmdInsertNewCase.Parameters.AddWithValue("@CaseStatus", caseDetail.Status);

                        cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                        cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                        cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                        if (caseDetail.NPF_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", caseDetail.NPF_ReceivedDate);
                        else cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", DBNull.Value);

                        cmdInsertNewCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                        cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                        cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                        if (caseDetail.IB_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", caseDetail.IB_ReceivedDate);
                        else cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", DBNull.Value);

                        cmdInsertNewCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                        cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                        cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                        if (caseDetail.POP_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", caseDetail.POP_ReceivedDate);
                        else cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                        cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form", caseDetail.MedicalRecord_Form);
                        cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                        cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                        if (caseDetail.MedRec_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", caseDetail.MedRec_ReceivedDate);
                        else cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", DBNull.Value);

                        cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                        cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                        cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                        if (caseDetail.Unknown_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", caseDetail.Unknown_ReceivedDate);
                        else cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", DBNull.Value);

                        cmdInsertNewCase.Parameters.AddWithValue("@Note", caseDetail.Note);
                        cmdInsertNewCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                        cmdInsertNewCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                        if (caseDetail.AddBill_Received_Date != null) cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", caseDetail.AddBill_Received_Date);
                        else cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", DBNull.Value);
                        if (caseDetail.Remove_Log == String.Empty) cmdInsertNewCase.Parameters.AddWithValue("@Remove_Log", DBNull.Value);
                        cmdInsertNewCase.Parameters.AddWithValue("@Individual_Id", caseDetail.Individual_Id);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nResult = cmdInsertNewCase.ExecuteNonQuery();
                        if (nResult == 1)
                        {
                            MessageBox.Show("The case has been saved.", "Information");

                            caseDetail.CaseId = txtCaseName.Text.Trim();
                            strCaseIdSelected = caseDetail.CaseId;
                            strContactIdSelected = caseDetail.ContactId;

                            btnNewMedBill_Case.Enabled = true;
                            btnEditMedBill.Enabled = true;
                            btnDeleteMedBill.Enabled = true;
                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();
                        //}
                        //else if (dlgResult == DialogResult.No)
                        //{
                        //    tbCMMManager.TabPages.Remove(tbpgCreateCase);
                        //    tbCMMManager.SelectedIndex = 2;

                        //    return;
                        //}
                        //else if (dlgResult == DialogResult.Cancel)
                        //{
                        //    return;
                        //}
                    }
                    else if (objCaseName != null)    // Edit and update case
                    {
                        //frmSaveChangeOnCase frmDlgSaveChange = new frmSaveChangeOnCase();

                        //DialogResult dlgResult = frmDlgSaveChange.ShowDialog();

                        ////if (frmDlgSaveChange.DialogResult == DialogResult.Yes)
                        //if (dlgResult == DialogResult.Yes)
                        //{
                        CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                        caseDetail.CaseId = txtCaseName.Text.Trim();
                        caseDetail.ContactId = String.Empty;
                        caseDetail.Individual_Id = String.Empty;
                        caseDetail.CreateDate = DateTime.Today;
                        //caseDetail.ModificationDate = DateTime.Today;
                        //caseDetail.CreateStaff = 8;     // WonJik
                        //caseDetail.ModifyingStaff = 8;  // WonJik
                        //caseDetail.CreateStaff = nLoggedUserId;
                        caseDetail.ModifyingStaff = nLoggedUserId;
                        caseDetail.Status = false;
                        caseDetail.Individual_Id = String.Empty;
                        caseDetail.NPF_Form = 0;
                        caseDetail.NPF_Form_File_Name = String.Empty;
                        caseDetail.NPF_Form_Destination_File_Name = String.Empty;
                        //caseDetail.NPF_ReceivedDate = DateTime.Today;
                        caseDetail.IB_Form = 0;
                        caseDetail.IB_Form_File_Name = String.Empty;
                        caseDetail.IB_Form_Destination_File_Name = String.Empty;
                        //caseDetail.IB_ReceivedDate = DateTime.Today;
                        caseDetail.POP_Form = 0;
                        caseDetail.POP_Form_File_Name = String.Empty;
                        caseDetail.POP_Form_Destionation_File_Name = String.Empty;
                        //caseDetail.POP_ReceivedDate = DateTime.Today;
                        caseDetail.MedicalRecord_Form = 0;
                        caseDetail.MedRec_Form_File_Name = String.Empty;
                        caseDetail.MedRec_Form_Destination_File_Name = String.Empty;
                        //caseDetail.MedRec_ReceivedDate = DateTime.Today;
                        caseDetail.Unknown_Form = 0;
                        caseDetail.Unknown_Form_File_Name = String.Empty;
                        caseDetail.Unknown_Form_Destination_File_Name = String.Empty;
                        //caseDetail.Unknown_ReceivedDate = DateTime.Today;
                        caseDetail.Note = String.Empty;
                        caseDetail.Log_Id = String.Empty;
                        caseDetail.AddBill_Form = false;
                        //caseDetail.AddBill_Received_Date = DateTime.Today;
                        caseDetail.Remove_Log = String.Empty;

                        if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                        if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                        if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();

                        if (chkNPF_CaseCreationPage.Checked)
                        {
                            caseDetail.NPF_Form = 1;
                            if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                            if (txtNPFUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                        }
                        if (chkIB_CaseCreationPage.Checked)
                        {
                            caseDetail.IB_Form = 1;
                            if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                            if (txtIBUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                        }
                        if (chkPoP_CaseCreationPage.Checked)
                        {
                            caseDetail.POP_Form = 1;
                            if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                            if (txtPoPUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                        }
                        if (chkMedicalRecordCaseCreationPage.Checked)
                        {
                            caseDetail.MedicalRecord_Form = 1;
                            if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                            if (txtMRUploadDate.Text.Trim() != String.Empty)
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                        }
                        if (chkOtherDocCaseCreationPage.Checked)
                        {
                            caseDetail.Unknown_Form = 1;
                            if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                            if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                            {
                                DateTime result;
                                if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                                else MessageBox.Show("Invalid DateTime value", "Error");
                            }
                            caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                        }

                        caseDetail.Note = txtNoteOnCase.Text.Trim();
                        caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                        caseDetail.AddBill_Form = true;
                        caseDetail.AddBill_Received_Date = DateTime.Today;
                        caseDetail.Remove_Log = String.Empty;

                        String strSqlUpdateCase = "Update [dbo].[tbl_case] set [dbo].[tbl_case].[ModifiDate] = @ModifiDate, [dbo].[tbl_case].[ModifiStaff] = @ModifiStaff, " +
                                                    "[dbo].[tbl_case].[NPF_Form] = @NPF_Form, [dbo].[tbl_case].[NPF_Form_File_Name] = @NPF_Form_File_Name, " +
                                                    "[dbo].[tbl_case].[NPF_Form_Destination_File_Name] = @NPF_Form_Destination_File_Name, [dbo].[tbl_case].[NPF_Receiv_Date] = @NPF_Receiv_Date, " +
                                                    "[dbo].[tbl_case].[IB_Form] = @IB_Form, [dbo].[tbl_case].[IB_Form_File_Name] = @IB_Form_File_Name, " +
                                                    "[dbo].[tbl_case].[IB_Form_Destination_File_Name] = @IB_Form_Destination_File_Name, [dbo].[tbl_case].[IB_Receiv_Date] = @IB_Receiv_Date, " +
                                                    "[dbo].[tbl_case].[POP_Form] = @POP_Form, [dbo].[tbl_case].[POP_Form_File_Name] = @POP_Form_File_Name, " +
                                                    "[dbo].[tbl_case].[POP_Form_Destination_File_Name] = @POP_Form_Destination_File_Name, [dbo].[tbl_case].[POP_Receiv_Date] = @POP_Receiv_Date, " +
                                                    "[dbo].[tbl_case].[MedRec_Form] = @MedRec_Form, [dbo].[tbl_case].[MedRec_Form_File_Name] = @MedRec_Form_File_Name, " +
                                                    "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name] = @MedRec_Form_Destination_File_Name, [dbo].[tbl_case].[MedRec_Receiv_Date] = @MedRec_Receiv_Date, " +
                                                    "[dbo].[tbl_case].[Unknown_Form] = @Unknown_Form, [dbo].[tbl_case].[Unknown_Form_File_Name] = @Unknown_Form_File_Name, " +
                                                    "[dbo].[tbl_case].[Unknown_Form_Destination_File_Name] = @Unknown_Form_Destination_File_Name, [dbo].[tbl_case].[Unknown_Receiv_Date] = @Unknown_Receiv_Date, " +
                                                    "[dbo].[tbl_case].[Note] = @CaseNote, [dbo].[tbl_case].[Log_ID] = @Log_Id, [dbo].[tbl_case].[AddBill_Form] = @AddBill_Form, " +
                                                    "[dbo].[tbl_case].[AddBill_Receiv_Date] = @AddBill_Receiv_Date, [dbo].[tbl_case].[Remove_Log] = @Remove_Log " +
                                                    "where [dbo].[tbl_case].[Case_Name] = @Case_Id";

                        SqlCommand cmdUpdateCase = new SqlCommand(strSqlUpdateCase, connRN);
                        cmdUpdateCase.CommandType = CommandType.Text;

                        cmdUpdateCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                        cmdUpdateCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                        cmdUpdateCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                        cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                        cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                        if (caseDetail.NPF_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", caseDetail.NPF_ReceivedDate);
                        else cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                        cmdUpdateCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                        cmdUpdateCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                        if (caseDetail.IB_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", caseDetail.IB_ReceivedDate);
                        else cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                        cmdUpdateCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                        cmdUpdateCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                        if (caseDetail.POP_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", caseDetail.POP_ReceivedDate);
                        else cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form", caseDetail.MedicalRecord_Form);
                        cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                        cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                        if (caseDetail.MedRec_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", caseDetail.MedRec_ReceivedDate);
                        else cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                        cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                        cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                        if (caseDetail.Unknown_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", caseDetail.Unknown_ReceivedDate);
                        else cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@CaseNote", caseDetail.Note);
                        cmdUpdateCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                        cmdUpdateCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                        if (caseDetail.AddBill_Received_Date != null) cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", caseDetail.AddBill_Received_Date);
                        else cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", DBNull.Value);

                        cmdUpdateCase.Parameters.AddWithValue("@Remove_Log", caseDetail.Remove_Log);

                        cmdUpdateCase.Parameters.AddWithValue("@Case_Id", caseDetail.CaseId);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nRowAffected = cmdUpdateCase.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        if (nRowAffected == 1)
                        {
                            MessageBox.Show("The change has been saved.", "Information");

                            btnNewMedBill_Case.Enabled = true;
                            btnEditMedBill.Enabled = true;
                            btnDeleteMedBill.Enabled = true;
                        }
                        else if (nRowAffected == 0) MessageBox.Show("The change has not been saved.", "Error");
                    }

                    tbCMMManager.TabPages.Remove(tbpgCreateCase);
                    tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCaseView"];
                    return;
                }
                else if (dlgResult == DialogResult.No)
                {
                    tbCMMManager.TabPages.Remove(tbpgCreateCase);
                    tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCaseView"];
                    return;
                }
                else if (dlgResult == DialogResult.Cancel)
                {
                    return;
                }
            }
            else return;

            //tbCMMManager.TabPages.Remove(tbpgCreateCase);
            //tbCMMManager.SelectedIndex = 3;
        }

        private void chkMedRecordReceived_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkMedRecord = sender as CheckBox;

            if (chkMedRecord.Checked)
            {
                dtpMedBillMedRecord.Format = DateTimePickerFormat.Short;
            }
            else
            {
                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
                dtpMedBillMedRecord.CustomFormat = " ";
            }
        }

        private void chkMedBillNPFReceived_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkNPF = sender as CheckBox;

            if (chkNPF.Checked)
            {
                dtpMedBillNPF.Format = DateTimePickerFormat.Short;
            }
            else
            {
                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
                dtpMedBillNPF.CustomFormat = " ";
            }
          
        }

        private void chkMedBill_IBReceived_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkIB = sender as CheckBox;

            if (chkIB.Checked)
            {
                dtpMedBill_IB.Format = DateTimePickerFormat.Short;
            }
            else
            {
                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
                dtpMedBill_IB.CustomFormat = " ";
            }
        }

        private void chkMedBillPOPReceived_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkPOP = sender as CheckBox;

            if (chkPOP.Checked) dtpMedBillPOP.Format = DateTimePickerFormat.Short;
            else
            {
                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
                dtpMedBillPOP.CustomFormat = " ";
            }
        }

        private void chkOtherDocReceived_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkOtherDoc = sender as CheckBox;

            if (chkOtherDoc.Checked) dtpMedBillOtherDoc.Format = DateTimePickerFormat.Short;
            else
            {
                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
                dtpMedBillOtherDoc.CustomFormat = " ";
            }
        }

        private void btnMedBill_Illness_Click(object sender, EventArgs e)
        {
            //String OldICD10Code = txtMedBill_Illness.Text.Trim();

            frmIllness frmIllnessList = new frmIllness();

            DateTime dtStartDate = DateTime.Parse(txtMembershipStartDate.Text.Trim());
            String IndividualId = txtIndividualID.Text;

            frmIllnessList.nLoggedInUserId = nLoggedUserId;

            frmIllnessList.strCaseIdIllness = strCaseIdForIllness;
            frmIllnessList.strIndividualId = IndividualId;

            frmIllnessList.IllnessSelected.IllnessId = Illness.IllnessId;
            frmIllnessList.IllnessSelected.ICD10Code = Illness.ICD10Code;
            //txtMedBill_CaseNo
            frmIllnessList.strCaseIdIllness = txtMedBill_CaseNo.Text.Trim();
            frmIllnessList.MembershipStartDate = dtStartDate;

            //frmIllnessList.ShowDialog(this);
            //frmIllnessList.Parent = this;
            frmIllnessList.ShowDialog(this);

            //if ((Boolean)gv[0, e.RowIndex].Value == true) gv[0, e.RowIndex].Value = false;

            //if (frmIllnessList.DialogResult == DialogResult.OK)
            if (frmIllnessList.SelectedOption == IllnessOption.Select)
            {
                //txtMedBill_Illness.Text = frmIllnessList.ICD10Code;

                Illness.IllnessId = frmIllnessList.IllnessSelected.IllnessId;
                Illness.ICD10Code = frmIllnessList.IllnessSelected.ICD10Code;

                txtMedBill_Illness.Text = Illness.ICD10Code;

                String strQueryForDiseaseName = "select [dbo].[ICD10 Code].[Name] from [dbo].[ICD10 Code] where [dbo].[ICD10 Code].[ICD10_CODE__C] = @ICD10Code";

                SqlCommand cmdQueryForDiseaseName = new SqlCommand(strQueryForDiseaseName, connSalesforce);
                cmdQueryForDiseaseName.CommandType = CommandType.Text;
                cmdQueryForDiseaseName.CommandText = strQueryForDiseaseName;

                cmdQueryForDiseaseName.Parameters.AddWithValue("@ICD10Code", Illness.ICD10Code);

                if (connSalesforce.State == ConnectionState.Open)
                {
                    connSalesforce.Close();
                    connSalesforce.Open();
                }
                else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

                SqlDataReader rdrDiseaseName = cmdQueryForDiseaseName.ExecuteReader();

                if (rdrDiseaseName.HasRows)
                {
                    rdrDiseaseName.Read();

                    txtMedBill_ICD10Code.Text = Illness.ICD10Code;
                    txtMedBillDiseaseName.Text = rdrDiseaseName.GetString(0);
                }
                if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                if (frmIllnessList.OldIllnessId != frmIllnessList.NewIllnessId)
                {
                    txtIncdProgram.Text = String.Empty;
                    txtMedBill_Incident.Text = String.Empty;
                }

                return;
            }
            else if (frmIllnessList.SelectedOption == IllnessOption.Close)
            {
                txtMedBill_Illness.Text = String.Empty;
                return;
            }
            else if (frmIllnessList.SelectedOption == IllnessOption.Cancel)
            {
                return;
            }
        }

        private void btnMedBill_Incident_Click(object sender, EventArgs e)
        {
            frmIncident frmIncidentList = new frmIncident();

            //frmIncidentList.StartPosition = FormStartPosition.CenterParent;

            //DialogResult dlgResult = frmIncidentList.ShowDialog();

            //String IncidentId = txtMedBill_Incident.Text.Trim();
            String IncidentId = String.Empty; //frmIncidentList.IncidentSelected.IncidentId;

            //if (dlgResult == DialogResult.OK)
            //{
            //    IncidentId = frmIncidentList.IncidentSelected.IncidentId;
            //}

            String IndividualId = txtIndividualID.Text.Trim();
            frmIncidentList.IncidentSelected.IncidentId = txtMedBill_Incident.Text.Trim();

            //String IncidentId = txtMedBill_Incident.Text.Trim();
            strCaseIdForIllness = txtMedBill_CaseNo.Text.Trim();

            if (txtMedBill_Illness.Text != String.Empty)
            {
                frmIncidentList.IndividualId = IndividualId;
                frmIncidentList.CaseId = strCaseIdForIllness;
                frmIncidentList.IllnessId = Illness.IllnessId;
                frmIncidentList.ICD10Code = Illness.ICD10Code;
                frmIncidentList.nLoggedInId = nLoggedUserId;
                //frmIncidentList.IncidentSelected.IncidentId = IncidentId;

                DialogResult dlgIncident = frmIncidentList.ShowDialog();

                //if (dlgIncident == DialogResult.OK)
                if (frmIncidentList.SelectedOption == IncidentOption.Select)
                {
                    txtMedBill_Incident.Text = frmIncidentList.IncidentSelected.IncidentId;
                    IncidentId = frmIncidentList.IncidentSelected.IncidentId;


                    //
                    // This section is needed to calculate personal responsibility

                    //String strSqlQueryForNewPRBalance = ""
                    //String IncidentNo = txtMedBill_Incident.Text.Trim();
                    //String IndividualId = txtCaseIndividualID.Text.Trim();

                    //String strSqlQueryForIncidentChange = "select [cdc].[dbo_tbl_incident_CT].[Program_id], [dbo].[tbl_program].[ProgramName] from [cdc].[dbo_tbl_incident_CT] " +
                    //                                      "inner join [dbo].[tbl_program] on [cdc].[dbo_tbl_incident_CT].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                    //                                      "where [cdc].[dbo_tbl_incident_CT].[Incident_id] = @IncidentId and [cdc].[dbo_tbl_incident_CT].[Individual_id] = @IndividualId and " +
                    //                                      "([cdc].[dbo_tbl_incident_CT].[__$operation] = 2 or [cdc].[dbo_tbl_incident_CT].[__$operation] = 3 or " +
                    //                                      "[cdc].[dbo_tbl_incident_CT].[__$operation] = 4) " +      // capture incident program for insert, update
                    //                                      "order by [cdc].[dbo_tbl_incident_CT].[Program_id]";

                    //SqlCommand cmdQueryForIncidentChange = new SqlCommand(strSqlQueryForIncidentChange, connRN);
                    //cmdQueryForIncidentChange.CommandType = CommandType.Text;

                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IncidentId", IncidentId);
                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //connRN.Open();
                    //SqlDataReader rdrIncidentChange = cmdQueryForIncidentChange.ExecuteReader();
                    //if (rdrIncidentChange.HasRows)
                    //{
                    //    while (rdrIncidentChange.Read())
                    //    {
                    //        //lstIncidentProgramInfo.Add(new IncidentProgramInfo { IncidentProgramId = rdrIncidentChange.GetInt16(0), IncidentProgramName = rdrIncidentChange.GetString(1).Trim() });
                    //        IncidentProgramInfo incidentProgram = new IncidentProgramInfo(rdrIncidentChange.GetInt16(0), rdrIncidentChange.GetString(1).Trim());
                    //        lstIncidentProgramInfo.Add(incidentProgram);
                    //    }
                    //}
                    //connRN.Close();

                    //Boolean bBronze = false;
                    //Boolean bSilver = false;
                    //Boolean bGold = false;
                    //Boolean bGoldPlus = false;
                    //Boolean bGoldMed1 = false;
                    //Boolean bGoldMed2 = false;

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.IncidentProgramId == 3)
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bBronze = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 2) && (bBronze == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bSilver = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 1) && (bBronze == false) && (bSilver == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 0) && (bBronze == false) && (bSilver == false) && (bGold == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 4) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 5) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false) && (bGoldMed1 == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.bPersonalResponsibilityProgram == true)
                    //        PersonalResponsibilityAmountInMedBill = incidentInfo.PersonalResponsibilityAmount;
                    //}


                    //Decimal PersonalResponsibilityAmount = 0;

                    //foreach (IncidentProgramInfo incdProgram in lstIncidentProgramInfo)
                    //{
                    //    if (incdProgram.bPersonalResponsibilityProgram == true) PersonalResponsibilityAmount = incdProgram.PersonalResponsibilityAmount;
                    //}

                    //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    //{
                    //    if (gvSettlementsInMedBill["PersonalResponsibility", i]?.Value != null)
                    //    {
                    //        Decimal result = 0;
                    //        if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                    //        {
                    //            PersonalResponsibilityAmount -= result;
                    //        }
                    //    }
                    //}

                    //txtPersonalResponsibility.Text = PersonalResponsibilityAmount.ToString("C");
                    //if (PersonalResponsibilityAmount < 0) txtPersonalResponsibility.BackColor = Color.Yellow;

                    String strSqlQueryForIncidentProgram = "select [dbo].[tbl_program].[ProgramName] from [dbo].[tbl_program] " +
                                                           "inner join [dbo].[tbl_incident] on [dbo].[tbl_program].[Program_Id] = [dbo].[tbl_incident].[Program_id] " +
                                                           "where [dbo].[tbl_incident].[Incident_id] = @IncidentId and [dbo].[tbl_incident].[Individual_id] = @IndividualId";

                    SqlCommand cmdQueryForIncidentProgram = new SqlCommand(strSqlQueryForIncidentProgram, connRN);
                    cmdQueryForIncidentProgram.CommandType = CommandType.Text;

                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IncidentId", IncidentId);
                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    Object objProgramName = cmdQueryForIncidentProgram.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String ProgramName = String.Empty;

                    if (objProgramName != null)
                    {
                        ProgramName = objProgramName.ToString();
                    }
                    else
                    {
                        MessageBox.Show("No Program name for the given incident: " + IncidentId, "Error", MessageBoxButtons.OK);
                        return;
                    }

                    if (ProgramName != String.Empty) txtIncdProgram.Text = ProgramName.Trim();

                    if (txtMemberProgram.Text.Trim() == txtIncdProgram.Text.Trim())
                    {
                        txtMemberProgram.BackColor = Color.White;
                        txtIncdProgram.BackColor = Color.White;
                    }
                    else
                    {
                        txtMemberProgram.BackColor = Color.Red;
                        txtIncdProgram.BackColor = Color.Red;
                    }
                    
                }
                //else if (dlgIncident == DialogResult.None)
                //else if (frmIncidentList.bIncidentSelected == false)
                else if (frmIncidentList.SelectedOption == IncidentOption.Close)
                {
                    txtIncdProgram.Text = String.Empty;
                    txtMedBill_Incident.Text = String.Empty;
                    return;
                }
                else if (frmIncidentList.SelectedOption == IncidentOption.Cancel)
                {
                    return;
                }
                //else if ((dlgIncident == DialogResult.Cancel)&&(frmIncidentList.bIncidentCanceled == true))
                //{

                //}
            }
            else
            {
                MessageBox.Show("No illess", "Information");
            }

        }

        private void btnMedBillCreatePgUpperSave_Click(object sender, EventArgs e)
        {
            if (txtMedBill_Illness.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Please select an Illness.", "Alert", MessageBoxButtons.OK);
                return;
            }
            if (txtMedBill_Incident.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Please select an Incident.", "Alert", MessageBoxButtons.OK);
                return;
            }
            if (txtMedBillAmount.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Please enter Medical Bill Amount.", "Alert", MessageBoxButtons.OK);
                return;
            }
            if (txtMedicalProvider.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Please select a Medical Provider.", "Alert", MessageBoxButtons.OK);
                return;
            }

            frmSaveNewMedBill frmSaveMedBill = new frmSaveNewMedBill();

            frmSaveMedBill.StartPosition = FormStartPosition.CenterParent;
            DialogResult dlgResult = frmSaveMedBill.ShowDialog();

            if (dlgResult == DialogResult.Yes)
            {
                String strMedBillNo = txtMedBillNo.Text.Trim();

                String strSqlQueryForMedBill = "select [dbo].[tbl_medbill].[BillNo] from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";

                SqlCommand cmdQueryForMedBill = new SqlCommand(strSqlQueryForMedBill, connRN);
                cmdQueryForMedBill.Parameters.AddWithValue("@MedBillNo", strMedBillNo);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                Object ResultMedBillNo = cmdQueryForMedBill.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                if (ResultMedBillNo == null)
                {
                    String strIndividualId = String.Empty;
                    String strCaseId = String.Empty;
                    String strBillStatus = String.Empty;
                    String strIllnessId = String.Empty;
                    String strIncidentId = String.Empty;

                    String strNewMedBillNo = String.Empty;
                    String strMedProvider = String.Empty;
                    String strPrescriptionName = String.Empty;
                    String strPrescriptionNo = String.Empty;
                    String strPrescriptionDescription = String.Empty;

                    if (txtIndividualIDMedBill.Text.Trim() != String.Empty) strIndividualId = txtIndividualIDMedBill.Text.Trim();
                    if (txtMedBill_CaseNo.Text.Trim() != String.Empty) strCaseId = txtMedBill_CaseNo.Text.Trim();
                    //if (txtMedicalBillStatus.Text.Trim() != String.Empty) strBillStatus = txtMedicalBillStatus.Text.Trim();
                   
                    if (txtMedBill_Illness.Text.Trim() != String.Empty) strIllnessId = Illness.IllnessId;
                    if (txtMedBill_Incident.Text.Trim() != String.Empty) strIncidentId = txtMedBill_Incident.Text.Trim();

                    if (txtMedBillNo.Text.Trim() != String.Empty) strNewMedBillNo = txtMedBillNo.Text.Trim();

                    String MedicalProvider = String.Empty;

                    if (txtMedicalProvider.Text.Trim() != String.Empty)
                    {
                        MedicalProvider = txtMedicalProvider.Text.Trim();
                    }
                    else
                    {
                        MessageBox.Show("Please enter the name of medical provider.", "Error");
                        return;
                    }

                    String PrescriptionName = String.Empty;

                    if (txtPrescriptionName.Text.Trim() != String.Empty)
                    {
                        PrescriptionName = txtPrescriptionName.Text.Trim();
                    }

                    String PrescriptionNo = String.Empty;

                    if (txtNumberOfMedication.Text.Trim() != String.Empty)
                    {
                        PrescriptionNo = txtNumberOfMedication.Text.Trim();
                    }

                    String PrescriptionDescription = String.Empty;

                    if (txtPrescriptionDescription.Text.Trim() != String.Empty)
                    {
                        PrescriptionDescription = txtPrescriptionDescription.Text.Trim();
                    }


                    int nPatientType = 0;   // default outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    //int nSelectedMedNote = cbMedicalBillNote1.SelectedIndex;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }



                    String strSqlInsertNewMedBill = "insert into dbo.tbl_medbill (IsDeleted, BillNo, MedBillType_Id, BillStatus, CreatedDate, CreatedById, LastModifiedDate, LastModifiedById, " +
                                                    "LastActivityDate, LastViewedDate, LastReferencedDate, Case_Id, Incident_Id, Illness_Id, BillAmount, SettlementTotal, " +
                                                    "Balance, BillDate, TotalSharedAmount, Individual_Id, Contact_Id, MedicalProvider_Id, PendingReason, " +
                                                    "Account_At_Provider, ProviderPhoneNumber, ProviderContactPerson, " +
                                                    "ProposalLetterSentDate, HIPPASentDate, MedicalRecordDate, " +
                                                    "ProofOfPaymentReceivedDate, IneligibleReason, OriginalPrescription, PersonalResponsibilityCredit, " +
                                                    "WellBeingCareTotal, WellBeingCare, Memo, DueDate, TotalNumberOfPhysicalTherapy, " +
                                                    "PrescriptionDrugName, PrescriptionNo, PrescriptionDescription, " +
                                                    "PatientTypeId, Note) " +
                                                    "values (@IsDeleted, @BillNo, @MedBillType_Id, @MedBillStatus, @CreatedDate, @CreateById, @LastModifiedDate, @LastModifiedById, " +
                                                    "@LastActivityDate, @LastViewedDate, @LastReferencedDate, @Case_Id, @Incident_Id, @Illness_Id, @BillAmount, @SettlementTotal, " +
                                                    "@Balance, @BillDate, @TotalSharedAmount, @Individual_Id, @Contact_Id, @MedicalProvider_Id, @PendingReason, " +
                                                    "@Account_At_Provider, @ProviderPhoneNo, @ProviderContactPerson, " +
                                                    "@ProposalLetterSentDate, @HIPPASentDate, @MedicalRecordDate, " +
                                                    "@ProofOfPaymentReceivedDate, @IneligibleReason, @OriginalPrescription, @PersonalResponsibilityCredit, " +
                                                    "@WellBeingCareTotal, @WellBeingCare, @Memo, @DueDate, @TotalNumberOfPhysicalTherapy, " +
                                                    "@PrescriptionDrugName, @PrescriptionNo, @PrescriptionDescription, " +
                                                    "@PatientTypeId, @Note)";

                    SqlCommand cmdInsertNewMedBill = new SqlCommand(strSqlInsertNewMedBill, connRN);
                    cmdInsertNewMedBill.CommandType = CommandType.Text;

                    cmdInsertNewMedBill.Parameters.AddWithValue("@IsDeleted", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillNo", strNewMedBillNo);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillStatus", comboMedBillStatus.SelectedIndex);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreatedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreateById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastViewedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastReferencedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Case_Id", strCaseId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Incident_Id", strIncidentId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Illness_Id", strIllnessId);


                    Decimal BillAmountResult = 0;
                    Decimal BillAmount = 0;

                    if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BillAmountResult))
                    {
                        BillAmount = BillAmountResult;
                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillAmount", BillAmount);
                    }
                    else
                    {
                        MessageBox.Show("Bill Amount is invalid.", "Error");
                        return;
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@SettlementTotal", 0);

                    Decimal BalanceResult = 0;
                    Decimal Balance = 0;

                    if (Decimal.TryParse(txtBalance.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BalanceResult))
                    {
                        Balance = BalanceResult;
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Balance", Balance);
                    }
                    else
                    {
                        MessageBox.Show("Balance is invalid.", "Error");
                        return;
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillDate", dtpBillDate.Value);

                    Decimal TotalSharedAmount = 0;
                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if ((gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "CMM Provider Payment") ||
                            (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Member Reimbursement"))
                            TotalSharedAmount += Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                        if (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Medical Provider Refund")
                            TotalSharedAmount -= Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@TotalSharedAmount", TotalSharedAmount);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Individual_Id", strIndividualId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Contact_Id", strIndividualId);
                    foreach (MedicalProviderInfo info in lstMedicalProvider)
                    {
                        if (info.Name == txtMedicalProvider.Text.Trim())
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalProvider_Id", info.ID);
                            break;
                        }
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@Account_At_Provider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@HIPPASentDate", dtpHippaSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalRecordDate", dtpMedicalRecordDate.Value);
                    //cmdInsertNewMedBill.Parameters.AddWithValue("@BillStatus", comboMedBillStatus.SelectedIndex);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProofOfPaymentReceivedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@OriginalPrescription", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@PersonalResponsibilityCredit", 500);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCareTotal", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCare", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Memo", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@DueDate", DateTime.Today);
                    
                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type : Medical Bill
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", nPatientType);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", comboPendingReason.SelectedIndex);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 1)   // Medical Bill Type : Prescription
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", txtPrescriptionDescription.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 2)   // Medical Bill Type : Physical Therapy
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short result = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                        {
                            nNumberOfPhysicalTherapy = result;
                            cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else
                        {
                            MessageBox.Show("Please enter a positive integer in the Number of Physical Therapy Text Box.", "Alert");
                            return;
                        }

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowInserted = cmdInsertNewMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowInserted == 1)
                    {
                        MessageBox.Show("The Medical Bill has been saved.", "Information");
                        btnAddNewSettlement.Enabled = true;
                        return;

                    }
                    else if (nRowInserted == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been saved.", "Error");
                        return;
                    }

                    bIsModified = false;

                }
                else if (ResultMedBillNo.ToString() == strMedBillNo)
                {
                    // update the med bill

                    if (txtIndividualIDMedBill.Text.Trim() == String.Empty)
                    {
                        MessageBox.Show("There is no illness code.", "Alert");
                        return;
                    }

                    if (txtMedBill_Incident.Text.Trim() == String.Empty)
                    {
                        MessageBox.Show("There is no incident id.", "Alert");
                        return;
                    }

                    String MedBillNo = txtMedBillNo.Text.Trim();
                    String IndividualId = txtIndividualIDMedBill.Text.Trim();

                    // Get illness id for ICD 10 Code
                    String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] " +
                                                        "where [dbo].[tbl_illness].[Individual_Id] = @IndividualId and [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                    SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                    cmdQueryForIllnessId.CommandType = CommandType.Text;

                    cmdQueryForIllnessId.Parameters.AddWithValue("@IndividualId", IndividualId);
                    cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", txtMedBill_Illness.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //int nIllnessId = Int32.Parse(cmdQueryForIllnessId.ExecuteScalar().ToString());
                    Object objIllnessId = cmdQueryForIllnessId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    int nResult;
                    int? nIllnessId = null;
                    if (objIllnessId != null)
                    {
                        if (Int32.TryParse(objIllnessId.ToString(), NumberStyles.Integer, new CultureInfo("en-US"), out nResult)) nIllnessId = nResult;
                    }
                    else
                    {
                        MessageBox.Show("No Illness Id for ICD 10 Code: " + nIllnessId.Value, "Error", MessageBoxButtons.OK);
                        return;
                    }
                    //else
                    //{
                    //    MessageBox.Show("Illness Id is empty.", "Alert");
                    //    return;
                    //}

                    // Get medical provider id
                    String strSqlQueryForMedicalProviderId = "select [dbo].[tbl_MedicalProvider].[ID] from [dbo].[tbl_MedicalProvider] where [dbo].[tbl_MedicalProvider].[Name] = @MedicalProviderName";

                    SqlCommand cmdQueryForMedicalProviderId = new SqlCommand(strSqlQueryForMedicalProviderId, connRN);
                    cmdQueryForMedicalProviderId.CommandType = CommandType.Text;

                    cmdQueryForMedicalProviderId.Parameters.AddWithValue("@MedicalProviderName", txtMedicalProvider.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String MedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar().ToString();
                    Object objMedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String MedicalProviderId = String.Empty;
                    if (objMedicalProviderId != null) MedicalProviderId = objMedicalProviderId.ToString();
                    else
                    {
                        MessageBox.Show("No Medical Provider Id for Medical Provider name: " + txtMedicalProvider.Text.Trim(), "Error", MessageBoxButtons.OK);
                        return;
                    }

                    int nPatientType = 0;   // default outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }

                    // Update the Medical Bill
                    String strSqlUpdateMedBill = "update [dbo].[tbl_medbill] set [dbo].[tbl_medbill].[LastModifiedDate] = @NewLastModifiedDate, " +
                                                 "[dbo].[tbl_medbill].[LastModifiedById] = @NewLastModifiedById, " +
                                                     "[dbo].[tbl_medbill].[Case_Id] = @NewCaseId, [dbo].[tbl_medbill].[Incident_Id] = @NewIncidentId, " +
                                                     "[dbo].[tbl_medbill].[Illness_Id] = @NewIllnessId, " +
                                                     "[dbo].[tbl_medbill].[BillAmount] = @NewBillAmount, [dbo].[tbl_medbill].[MedBillType_Id] = @NewMedBillType_Id, " +
                                                     "[dbo].[tbl_medbill].[BillStatus] = @NewMedBillStatus, " +
                                                     "[dbo].[tbl_medbill].[SettlementTotal] = @NewSettlementTotal, [dbo].[tbl_medbill].[Balance] = @NewBalance, " +
                                                     "[dbo].[tbl_medbill].[BillDate] = @NewBillDate, [dbo].[tbl_medbill].[DueDate] = @NewDueDate, [dbo].[tbl_medbill].[TotalSharedAmount] = @NewTotalSharedAmount, " +
                                                     "[dbo].[tbl_medbill].[Guarantor] = @NewGuarantor, " +
                                                     "[dbo].[tbl_medbill].[MedicalProvider_Id] = @NewMedicalProviderId, " +
                                                     "[dbo].[tbl_medbill].[Account_At_Provider] = @NewAccountAtProvider, " +
                                                     "[dbo].[tbl_medbill].[ProviderPhoneNumber] = @NewProviderPhoneNo, " +
                                                     "[dbo].[tbl_medbill].[ProviderContactPerson] = @NewProviderContactPerson, " +
                                                     "[dbo].[tbl_medbill].[ProposalLetterSentDate] = @NewProposalLetterSentDate, " +
                                                     "[dbo].[tbl_medbill].[HIPPASentDate] = @NewHIPPASentDate, " +
                                                     "[dbo].[tbl_medbill].[MedicalRecordDate] = @NewMedicalRecordDate, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDrugName] = @NewPrescriptionDrugName, [dbo].[tbl_medbill].[PrescriptionNo] = @NewPrescriptionNo, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDescription] = @NewPrescriptionDescription, " +
                                                     "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy] = @NewTotalNumberOfPhysicalTherapy, " +
                                                     "[dbo].[tbl_medbill].[PatientTypeId] = @NewPatientTypeId, " +
                                                     "[dbo].[tbl_medbill].[Note] = @Note, " +
                                                     "[dbo].[tbl_medbill].[WellBeingCareTotal] = @NewWellBeingCareTotal, [dbo].[tbl_medbill].[WellBeingCare] = @NewWellBeingCare, " +
                                                     "[dbo].[tbl_medbill].[IneligibleReason] = @NewIneligibleReason, [dbo].[tbl_medbill].[PendingReason] = @NewPendingReason, " +
                                                     "[dbo].[tbl_medbill].[OriginalPrescription] = @NewOriginalPrescription " +
                                                     "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

                    SqlCommand cmdUpdateMedBill = new SqlCommand(strSqlUpdateMedBill, connRN);
                    cmdUpdateMedBill.CommandType = CommandType.Text;

                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedDate", DateTime.Today.ToString("MM/dd/yyyy"));
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedById", nLoggedUserId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewCaseId", txtMedBill_CaseNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIncidentId", txtMedBill_Incident.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIllnessId", nIllnessId.Value);
                    Decimal BillAmount = 0;
                    Decimal BillAmountResult = 0;

                    if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BillAmountResult))
                    {
                        BillAmount = BillAmountResult;
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewBillAmount", BillAmount);
                    }
                    else
                    {
                        MessageBox.Show("Bill Amount is invalid.", "Error");
                        return;
                    }
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillStatus", comboMedBillStatus.SelectedIndex);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewSettlementTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBalance", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBillDate", dtpBillDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewDueDate", dtpDueDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalSharedAmount", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewGuarantor", txtMedBillGuarantor.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalProviderId", MedicalProviderId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewAccountAtProvider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewHIPPASentDate", dtpHippaSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalRecordDate", dtpMedicalRecordDate.Value);

                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type - Medical Bill
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", nPatientType);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", comboPendingReason.SelectedIndex);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);

                    }
                    if (comboMedBillType.SelectedIndex == 1)        // Medical Bill Type - Prescription
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", txtPrescriptionDescription.Text.Trim());

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    if (comboMedBillType.SelectedIndex == 2)        // Medical Bill Type - Physical Therapy
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short NumPhysicalTherapyResult = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out NumPhysicalTherapyResult))
                        {
                            nNumberOfPhysicalTherapy = NumPhysicalTherapyResult;
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else
                        {
                            MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Error");
                            return;
                        }

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }


                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCareTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCare", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewOriginalPrescription", String.Empty);
                    cmdUpdateMedBill.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                    cmdUpdateMedBill.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nAffectedRow = cmdUpdateMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nAffectedRow == 1)
                    {
                        MessageBox.Show("The Medical Bill has been updated.", "Information");
                        return;
                    }
                    else if (nAffectedRow == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been updated.", "Error");
                        return;
                    }

                    bIsModified = false;
                }
            }
            else if (dlgResult == DialogResult.No)
            {
                //tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                //tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];
                return;
            }
            //else if (dlgResult == DialogResult.Cancel)
            //{
            //    return;
            //}
        }

        private void btnBrowseNPF_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSourceFileDlg = new OpenFileDialog();

            OpenSourceFileDlg.Filter = "JPG Files | *.jpg; *.jpeg | PDF Files | *.pdf";
            OpenSourceFileDlg.DefaultExt = "jpg";
            OpenSourceFileDlg.RestoreDirectory = true;

            if (OpenSourceFileDlg.ShowDialog() == DialogResult.OK)
            {
                strNPFormFilePathSource = OpenSourceFileDlg.FileName;
                strNPFormFilePathDestination = strDestinationPath + "_NPF_" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss") + "_" + OpenSourceFileDlg.SafeFileName;
                txtNPFFormFilePath.Text = strNPFormFilePathSource;
                btnNPFFormUpload.Enabled = true;
                return;
            }
            else return;
        }

        private void btnViewNPF_Click(object sender, EventArgs e)
        {
            if (chkMedBillNPFReceived.Checked)
            {
                String CaseName = strCaseIdSelected;
                String ContactId = strContactIdSelected;

                String strSqlQueryForNPFForm = "select dbo.tbl_case.NPF_Form_Destination_File_Name from dbo.tbl_case where dbo.tbl_case.Case_Name = @Case_Name and dbo.tbl_case.Contact_ID = @ContactId";

                SqlCommand cmdQueryForNPFForm = new SqlCommand(strSqlQueryForNPFForm, connRN);
                cmdQueryForNPFForm.CommandType = CommandType.Text;

                cmdQueryForNPFForm.Parameters.AddWithValue("@Case_Name", CaseName);
                cmdQueryForNPFForm.Parameters.AddWithValue("@ContactId", ContactId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String NPF_FileName = cmdQueryForNPFForm.ExecuteScalar() as String;
                Object objNPF_FileName = cmdQueryForNPFForm.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String NPF_FileName = String.Empty;

                if (objNPF_FileName != null) NPF_FileName = objNPF_FileName.ToString();
                else
                {
                    MessageBox.Show("No NPF Form Destination File Name for Case Name: " + CaseName, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (NPF_FileName != String.Empty)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = NPF_FileName;

                    Process.Start(psi);
                }
            }
        }

        private void btnViewIB_Click(object sender, EventArgs e)
        {
            if (chkMedBill_IBReceived.Checked)
            {
                String CaseName = strCaseIdSelected;
                String ContactId = strContactIdSelected;

                String strSqlQueryForIBForm = "select dbo.tbl_case.IB_Form_Destination_File_Name from dbo.tbl_case where dbo.tbl_case.Case_Name = @Case_Name and dbo.tbl_case.Contact_ID = @ContactId";

                SqlCommand cmdQueryForIBForm = new SqlCommand(strSqlQueryForIBForm, connRN);
                cmdQueryForIBForm.CommandType = CommandType.Text;

                cmdQueryForIBForm.Parameters.AddWithValue("@Case_Name", CaseName);
                cmdQueryForIBForm.Parameters.AddWithValue("@ContactId", ContactId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String IB_FileName = cmdQueryForIBForm.ExecuteScalar() as String;
                Object objIB_FileName = cmdQueryForIBForm.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String IB_FileName = String.Empty;
                if (objIB_FileName != null) IB_FileName = objIB_FileName.ToString();
                else
                {
                    MessageBox.Show("No IB Form Destination File Name for Case Name: " + CaseName, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (IB_FileName != String.Empty)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = IB_FileName;

                    Process.Start(psi);
                }
            }
        }

        private void btnViewPoP_Click(object sender, EventArgs e)
        {
            if (chkMedBillPOPReceived.Checked)
            {
                String CaseName = strCaseIdSelected;
                String ContactId = strContactIdSelected;

                String strSqlQueryForPOPForm = "select dbo.tbl_case.POP_Form_Destination_File_Name from dbo.tbl_case where dbo.tbl_case.Case_Name = @Case_Name and dbo.tbl_case.Contact_ID = @ContactId";

                SqlCommand cmdQueryForPOPForm = new SqlCommand(strSqlQueryForPOPForm, connRN);
                cmdQueryForPOPForm.CommandType = CommandType.Text;

                cmdQueryForPOPForm.Parameters.AddWithValue("@Case_Name", CaseName);
                cmdQueryForPOPForm.Parameters.AddWithValue("@ContactId", ContactId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String POP_FileName = cmdQueryForPOPForm.ExecuteScalar() as String;
                Object objPOP_FileName = cmdQueryForPOPForm.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String POP_FileName = String.Empty;
                if (objPOP_FileName != null) POP_FileName = objPOP_FileName.ToString();
                else
                {
                    MessageBox.Show("No POP Form Destination File Name for Case Name: " + CaseName, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (POP_FileName != String.Empty)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = POP_FileName;

                    Process.Start(psi);
                }
            }
        }

        private void btnViewMedRecord_Click(object sender, EventArgs e)
        {
            if (chkMedRecordReceived.Checked)
            {
                String CaseName = strCaseIdSelected;
                String ContactId = strContactIdSelected;

                String strSqlQueryForMedRecordForm = "select dbo.tbl_case.MedRec_Form_Destination_File_Name from dbo.tbl_case where dbo.tbl_case.Case_Name = @Case_Name and dbo.tbl_case.Contact_ID = @ContactId";

                SqlCommand cmdQueryForMedRecordForm = new SqlCommand(strSqlQueryForMedRecordForm, connRN);
                cmdQueryForMedRecordForm.CommandType = CommandType.Text;

                cmdQueryForMedRecordForm.Parameters.AddWithValue("@Case_Name", CaseName);
                cmdQueryForMedRecordForm.Parameters.AddWithValue("@ContactId", ContactId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String MedRecord_FileName = cmdQueryForMedRecordForm.ExecuteScalar() as String;
                Object objMedRecord_FileName = cmdQueryForMedRecordForm.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String MedRecord_FileName = String.Empty;
                if (objMedRecord_FileName != null) MedRecord_FileName = objMedRecord_FileName.ToString();
                else
                {
                    MessageBox.Show("No Med Rec Form Destination File Name for Case Name: " + CaseName, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (MedRecord_FileName != String.Empty)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = MedRecord_FileName;

                    Process.Start(psi);
                }
            }
        }

        private void btnViewOtherDoc_Click(object sender, EventArgs e)
        {
            if (chkOtherDocReceived.Checked)
            {
                String CaseName = strCaseIdSelected;
                String ContactId = strContactIdSelected;

                String strSqlQueryForUnknownDocForm = "select dbo.tbl_case.Unknown_Form_Destination_File_Name from dbo.tbl_case where dbo.tbl_case.Case_Name = @Case_Name and dbo.tbl_case.Contact_ID = @ContactId";

                SqlCommand cmdQueryForUnknownDocForm = new SqlCommand(strSqlQueryForUnknownDocForm, connRN);
                cmdQueryForUnknownDocForm.CommandType = CommandType.Text;

                cmdQueryForUnknownDocForm.Parameters.AddWithValue("@Case_Name", CaseName);
                cmdQueryForUnknownDocForm.Parameters.AddWithValue("@ContactId", ContactId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //String UnknownDoc_FileName = cmdQueryForUnknownDocForm.ExecuteScalar() as String;
                Object objUnknownDoc_FileName = cmdQueryForUnknownDocForm.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                String UnknownDoc_FileName = String.Empty;
                if (objUnknownDoc_FileName != null) UnknownDoc_FileName = objUnknownDoc_FileName.ToString();
                else
                {
                    MessageBox.Show("No Unknown Form Destination File Name for Case Name: " + CaseName, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (UnknownDoc_FileName != String.Empty)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = UnknownDoc_FileName;

                    Process.Start(psi);
                }
            }
        }

        public String MedBillNote(int med_bill_note_id)
        {
            if (med_bill_note_id >= 0)
            {
                String strMedBillNote = String.Empty;

                String strSqlQueryForMedicalNote1Value = "select dbo.tbl_MedBillNoteType.MedBillNoteTypeValue from dbo.tbl_MedBillNoteType " +
                                                         "where dbo.tbl_MedBillNoteType.MedBillNoteTypeId = @MedBillNoteTypeId";

                SqlCommand cmdQueryForMedicalNote1 = new SqlCommand(strSqlQueryForMedicalNote1Value, connRN);
                cmdQueryForMedicalNote1.CommandType = CommandType.Text;

                cmdQueryForMedicalNote1.Parameters.AddWithValue("@MedBillNoteTypeId", med_bill_note_id);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                //strMedBillNote = cmdQueryForMedicalNote1.ExecuteScalar().ToString();
                Object objMedBillNote = cmdQueryForMedicalNote1.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                if (objMedBillNote != null) strMedBillNote = objMedBillNote.ToString();
                else
                {
                    //MessageBox.Show("No Medical Note Type")
                }

                return strMedBillNote;
            }
            else return String.Empty;
        }

        private void btnMedBillCreationPgUpperCancel_Click(object sender, EventArgs e)
        {

            DialogResult dlgClose = MessageBox.Show("Do you want to close Medical Bill Page?", "Alert", MessageBoxButtons.YesNo);

            if (dlgClose == DialogResult.Yes)
            {
                DialogResult dlgResult = MessageBox.Show("Do you want save the change?", "Alert", MessageBoxButtons.YesNoCancel);

                if (dlgResult == DialogResult.Yes)
                {
                    String strMedBillNo = txtMedBillNo.Text.Trim();

                    String strSqlQueryForMedBill = "select [dbo].[tbl_medbill].[BillNo] from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";

                    SqlCommand cmdQueryForMedBill = new SqlCommand(strSqlQueryForMedBill, connRN);
                    cmdQueryForMedBill.Parameters.AddWithValue("@MedBillNo", strMedBillNo);

                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    Object ResultMedBillNo = cmdQueryForMedBill.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (ResultMedBillNo == null)
                    {
                        String strIndividualId = String.Empty;
                        String strCaseId = String.Empty;
                        String strBillStatus = String.Empty;
                        String strIllnessId = String.Empty;
                        String strIncidentId = String.Empty;

                        String strNewMedBillNo = String.Empty;
                        String strMedProvider = String.Empty;
                        String strPrescriptionName = String.Empty;
                        String strPrescriptionNo = String.Empty;
                        String strPrescriptionDescription = String.Empty;

                        if (txtIndividualIDMedBill.Text.Trim() != String.Empty) strIndividualId = txtIndividualIDMedBill.Text.Trim();
                        if (txtMedBill_CaseNo.Text.Trim() != String.Empty) strCaseId = txtMedBill_CaseNo.Text.Trim();
                        //if (txtMedicalBillStatus.Text.Trim() != String.Empty) strBillStatus = txtMedicalBillStatus.Text.Trim();

                        if (txtMedBill_Illness.Text.Trim() != String.Empty) strIllnessId = Illness.IllnessId;
                        if (txtMedBill_Incident.Text.Trim() != String.Empty) strIncidentId = txtMedBill_Incident.Text.Trim();

                        if (txtMedBillNo.Text.Trim() != String.Empty) strNewMedBillNo = txtMedBillNo.Text.Trim();

                        String MedicalProvider = String.Empty;

                        if (txtMedicalProvider.Text.Trim() != String.Empty)
                        {
                            MedicalProvider = txtMedicalProvider.Text.Trim();
                        }

                        String PrescriptionName = String.Empty;

                        if (txtPrescriptionName.Text.Trim() != String.Empty)
                        {
                            PrescriptionName = txtPrescriptionName.Text.Trim();
                        }

                        String PrescriptionNo = String.Empty;

                        if (txtNumberOfMedication.Text.Trim() != String.Empty)
                        {
                            PrescriptionNo = txtNumberOfMedication.Text.Trim();
                        }

                        String PrescriptionDescription = String.Empty;

                        if (txtPrescriptionDescription.Text.Trim() != String.Empty)
                        {
                            PrescriptionDescription = txtPrescriptionDescription.Text.Trim();
                        }


                        int nPatientType = 0;   // default outpatient

                        if (rbOutpatient.Checked) nPatientType = 0;
                        else if (rbInpatient.Checked) nPatientType = 1;

                        //int nSelectedMedNote = cbMedicalBillNote1.SelectedIndex;

                        String strNote = String.Empty;

                        if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                        {
                            strNote = txtMedBillNote.Text.Trim();
                        }
                        else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                        {
                            strNote = txtPrescriptionNote.Text.Trim();
                        }
                        else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                        {
                            strNote = txtPhysicalTherapyRxNote.Text.Trim();
                        }



                        String strSqlInsertNewMedBill = "insert into dbo.tbl_medbill (BillNo, MedBillType_Id, CreatedDate, CreatedById, LastModifiedDate, LastModifiedById, " +
                                                        "LastActivityDate, LastViewedDate, LastReferencedDate, Case_Id, Incident_Id, Illness_Id, BillAmount, SettlementTotal, " +
                                                        "Balance, BillDate, TotalSharedAmount, Individual_Id, Contact_Id, MedicalProvider_Id, PendingReason, " +
                                                        "Account_At_Provider, ProviderPhoneNumber, ProviderContactPerson, " +
                                                        "ProposalLetterSentDate, HIPPASentDate, MedicalRecordDate, " +
                                                        "BillStatus, ProofOfPaymentReceivedDate, IneligibleReason, OriginalPrescription, PersonalResponsibilityCredit, " +
                                                        "WellBeingCareTotal, WellBeingCare, Memo, DueDate, TotalNumberOfPhysicalTherapy, " +
                                                        "PrescriptionDrugName, PrescriptionNo, PrescriptionDescription, " +
                                                        "PatientTypeId, Note) " +
                                                        "values (@BillNo, @MedBillType_Id, @CreatedDate, @CreateById, @LastModifiedDate, @LastModifiedById, " +
                                                        "@LastActivityDate, @LastViewedDate, @LastReferencedDate, @Case_Id, @Incident_Id, @Illness_Id, @BillAmount, @SettlementTotal, " +
                                                        "@Balance, @BillDate, @TotalSharedAmount, @Individual_Id, @Contact_Id, @MedicalProvider_Id, @PendingReason, " +
                                                        "@Account_At_Provider, @ProviderPhoneNo, @ProviderContactPerson, " +
                                                        "@ProposalLetterSentDate, @HIPPASentDate, @MedicalRecordDate, " +
                                                        "@BillStatus, @ProofOfPaymentReceivedDate, @IneligibleReason, @OriginalPrescription, @PersonalResponsibilityCredit, " +
                                                        "@WellBeingCareTotal, @WellBeingCare, @Memo, @DueDate, @TotalNumberOfPhysicalTherapy, " +
                                                        "@PrescriptionDrugName, @PrescriptionNo, @PrescriptionDescription, " +
                                                        "@PatientTypeId, @Note)";

                        SqlCommand cmdInsertNewMedBill = new SqlCommand(strSqlInsertNewMedBill, connRN);
                        cmdInsertNewMedBill.CommandType = CommandType.Text;

                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillNo", strNewMedBillNo);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillType_Id", comboMedBillType.SelectedIndex + 1);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@CreatedDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@CreateById", nLoggedUserId);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedById", nLoggedUserId);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@LastViewedDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@LastReferencedDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Case_Id", strCaseId);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Incident_Id", strIncidentId);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Illness_Id", strIllnessId);
                        Decimal dBillAmount = 0;
                        Decimal BillAmount = 0;

                        if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out dBillAmount))
                        {
                            BillAmount = dBillAmount;
                            cmdInsertNewMedBill.Parameters.AddWithValue("@BillAmount", BillAmount);
                        }
                        else
                        {
                            MessageBox.Show("Bill Amount should be currency value.", "Error", MessageBoxButtons.OK);
                            return;
                        }
                        cmdInsertNewMedBill.Parameters.AddWithValue("@SettlementTotal", 0);

                        Decimal dBalance = 0;
                        if (!Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out dBalance))
                        {
                            MessageBox.Show("Balance should be currency value.", "Error");
                        }
                        else
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@Balance", Decimal.Parse(txtBalance.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US")));
                        }

                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillDate", dtpBillDate.Value);

                        Decimal TotalSharedAmount = 0;
                        for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                        {
                            if ((gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "CMM Provider Payment") ||
                                (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Member Reimbursement"))
                                TotalSharedAmount += Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                            if (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Medical Provider Refund")
                                TotalSharedAmount -= Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                        }

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalSharedAmount", TotalSharedAmount);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Individual_Id", strIndividualId);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Contact_Id", strIndividualId);
                        foreach (MedicalProviderInfo info in lstMedicalProvider)
                        {
                            if (info.Name == txtMedicalProvider.Text.Trim())
                            {
                                cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalProvider_Id", info.ID);
                                break;
                            }
                        }

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Account_At_Provider", txtMedBillAccountNoAtProvider.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderContactPerson", txtProviderContactPerson.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@ProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@HIPPASentDate", dtpHippaSentDate.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalRecordDate", dtpMedicalRecordDate.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillStatus", comboMedBillStatus.SelectedIndex);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@ProofOfPaymentReceivedDate", DateTime.Today);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@OriginalPrescription", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PersonalResponsibilityCredit", 500);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCareTotal", 0);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCare", 0);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Memo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@DueDate", DateTime.Today);

                        if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type : Medical Bill
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", nPatientType);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", comboPendingReason.SelectedIndex);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", comboIneligibleReason.SelectedIndex);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                        }
                        else if (comboMedBillType.SelectedIndex == 1)   // Medical Bill Type : Prescription
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", txtPrescriptionName.Text.Trim());
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", txtNumberOfMedication.Text.Trim());
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", txtPrescriptionDescription.Text.Trim());
                            cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                        }
                        else if (comboMedBillType.SelectedIndex == 2)   // Medical Bill Type : Physical Therapy
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                            cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                            cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                            int nNumberOfPhysicalTherapy = 0;
                            short result = 0;
                            if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                            {
                                nNumberOfPhysicalTherapy = result;
                                cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                            }
                            else MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Alert");

                            cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                        }

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nRowInserted = cmdInsertNewMedBill.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        if (nRowInserted == 1)
                        {
                            MessageBox.Show("The Medical Bill has been saved.", "Information");
                        }
                        else if (nRowInserted == 0)
                        {
                            MessageBox.Show("The Medical Bill has not been saved.", "Error");
                        }

                        bIsModified = false;

                        tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                        tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];

                    }
                    else if (ResultMedBillNo.ToString() == strMedBillNo)
                    {
                        // update the med bill

                        String MedBillNo = txtMedBillNo.Text.Trim();
                        String IndividualId = txtIndividualIDMedBill.Text.Trim();

                        // Get illness id for ICD 10 Code
                        String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] " +
                                                            "where [dbo].[tbl_illness].[Individual_Id] = @IndividualId and [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                        SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                        cmdQueryForIllnessId.CommandType = CommandType.Text;

                        cmdQueryForIllnessId.Parameters.AddWithValue("@IndividualId", IndividualId);
                        cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", txtMedBill_Illness.Text.Trim());

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        //int nIllnessId = Int32.Parse(cmdQueryForIllnessId.ExecuteScalar().ToString());
                        Object objIllnessId = cmdQueryForIllnessId.ExecuteScalar();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        int nIllnessId = 0;
                        int nIllnessIdResult = 0;
                        if (objIllnessId != null)
                        {
                            if (Int32.TryParse(objIllnessId.ToString(), NumberStyles.Integer, new CultureInfo("en-US"), out nIllnessIdResult)) nIllnessId = nIllnessIdResult;
                        }
                        else
                        {
                            MessageBox.Show("No Illness Id for ICD 10 Code: " + txtMedBill_Illness.Text.Trim(), "Error", MessageBoxButtons.OK);
                            return;
                        }

                        // Get medical provider id
                        String strSqlQueryForMedicalProviderId = "select [dbo].[tbl_MedicalProvider].[ID] from [dbo].[tbl_MedicalProvider] where [dbo].[tbl_MedicalProvider].[Name] = @MedicalProviderName";

                        SqlCommand cmdQueryForMedicalProviderId = new SqlCommand(strSqlQueryForMedicalProviderId, connRN);
                        cmdQueryForMedicalProviderId.CommandType = CommandType.Text;

                        cmdQueryForMedicalProviderId.Parameters.AddWithValue("@MedicalProviderName", txtMedicalProvider.Text.Trim());

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        //String MedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar().ToString();
                        Object objMedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        String MedicalProviderId = String.Empty;
                        if (objMedicalProviderId != null) MedicalProviderId = objMedicalProviderId.ToString();
                        else
                        {
                            MessageBox.Show("No Medical Provider Id for Medical Provider Name: " + txtMedicalProvider.Text.Trim(), "Error", MessageBoxButtons.OK);
                            return;
                        }

                        int nPatientType = 0;   // default outpatient

                        if (rbOutpatient.Checked) nPatientType = 0;
                        else if (rbInpatient.Checked) nPatientType = 1;

                        String strNote = String.Empty;

                        if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                        {
                            strNote = txtMedBillNote.Text.Trim();
                        }
                        else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                        {
                            strNote = txtPrescriptionNote.Text.Trim();
                        }
                        else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                        {
                            strNote = txtPhysicalTherapyRxNote.Text.Trim();
                        }

                        // Update the Medical Bill
                        String strSqlUpdateMedBill = "update [dbo].[tbl_medbill] set [dbo].[tbl_medbill].[LastModifiedDate] = @NewLastModifiedDate, [dbo].[tbl_medbill].[LastModifiedById] = @NewLastModifiedById, " +
                                                         "[dbo].[tbl_medbill].[Case_Id] = @NewCaseId, [dbo].[tbl_medbill].[Incident_Id] = @NewIncidentId, [dbo].[tbl_medbill].[Illness_Id] = @NewIllnessId, " +
                                                         "[dbo].[tbl_medbill].[BillAmount] = @NewBillAmount, [dbo].[tbl_medbill].[MedBillType_Id] = @NewMedBillType_Id, " +
                                                         "[dbo].[tbl_medbill].[SettlementTotal] = @NewSettlementTotal, [dbo].[tbl_medbill].[Balance] = @NewBalance, " +
                                                         "[dbo].[tbl_medbill].[BillDate] = @NewBillDate, [dbo].[tbl_medbill].[DueDate] = @NewDueDate, [dbo].[tbl_medbill].[TotalSharedAmount] = @NewTotalSharedAmount, " +
                                                         "[dbo].[tbl_medbill].[Guarantor] = @NewGuarantor, " +
                                                         "[dbo].[tbl_medbill].[MedicalProvider_Id] = @NewMedicalProviderId, " +
                                                         "[dbo].[tbl_medbill].[Account_At_Provider] = @NewAccountAtProvider, " +
                                                         "[dbo].[tbl_medbill].[ProviderPhoneNumber] = @NewProviderPhoneNo, " +
                                                         "[dbo].[tbl_medbill].[ProviderContactPerson] = @NewProviderContactPerson, " +
                                                         "[dbo].[tbl_medbill].[ProposalLetterSentDate] = @NewProposalLetterSentDate, " +
                                                         "[dbo].[tbl_medbill].[HIPPASentDate] = @NewHIPPASentDate, " +
                                                         "[dbo].[tbl_medbill].[MedicalRecordDate] = @NewMedicalRecordDate, " +
                                                         "[dbo].[tbl_medbill].[PrescriptionDrugName] = @NewPrescriptionDrugName, [dbo].[tbl_medbill].[PrescriptionNo] = @NewPrescriptionNo, " +
                                                         "[dbo].[tbl_medbill].[PrescriptionDescription] = @NewPrescriptionDescription, " +
                                                         "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy] = @NewTotalNumberOfPhysicalTherapy, " +
                                                         "[dbo].[tbl_medbill].[PatientTypeId] = @NewPatientTypeId, " +
                                                         "[dbo].[tbl_medbill].[Note] = @Note, " +
                                                         "[dbo].[tbl_medbill].[WellBeingCareTotal] = @NewWellBeingCareTotal, [dbo].[tbl_medbill].[WellBeingCare] = @NewWellBeingCare, " +
                                                         "[dbo].[tbl_medbill].[IneligibleReason] = @NewIneligibleReason, [dbo].[tbl_medbill].[PendingReason] = @NewPendingReason, " +
                                                         "[dbo].[tbl_medbill].[OriginalPrescription] = @NewOriginalPrescription " +
                                                         "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

                        SqlCommand cmdUpdateMedBill = new SqlCommand(strSqlUpdateMedBill, connRN);
                        cmdUpdateMedBill.CommandType = CommandType.Text;

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedDate", DateTime.Today.ToString("MM/dd/yyyy"));
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedById", nLoggedUserId);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewCaseId", txtMedBill_CaseNo.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIncidentId", txtMedBill_Incident.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIllnessId", nIllnessId);
                        Decimal resultBillAmount = 0;
                        Decimal BillAmount = 0;
                        if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out resultBillAmount))
                        {
                            BillAmount = resultBillAmount;
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewBillAmount", BillAmount);
                        }
                        else
                        {
                            MessageBox.Show("Invalid Bill Amount", "Error", MessageBoxButtons.OK);
                            return;
                        }
                        //cmdUpdateMedBill.Parameters.AddWithValue("@NewBillAmount", Decimal.Parse(txtMedBillAmount.Text.Substring(1).Trim()));
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillType_Id", comboMedBillType.SelectedIndex + 1);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewSettlementTotal", 0);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewBalance", 0);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewBillDate", dtpBillDate.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewDueDate", dtpDueDate.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalSharedAmount", 0);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewGuarantor", txtMedBillGuarantor.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalProviderId", MedicalProviderId);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewAccountAtProvider", txtMedBillAccountNoAtProvider.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderContactPerson", txtProviderContactPerson.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewHIPPASentDate", dtpHippaSentDate.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalRecordDate", dtpMedicalRecordDate.Value);

                        if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type - Medical Bill
                        {
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                            cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", nPatientType);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", comboPendingReason.SelectedIndex);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", comboIneligibleReason.SelectedIndex);

                            cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);

                        }
                        if (comboMedBillType.SelectedIndex == 1)        // Medical Bill Type - Prescription
                        {
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                            cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", txtPrescriptionName.Text.Trim());
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", txtNumberOfMedication.Text.Trim());
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", txtPrescriptionDescription.Text.Trim());

                            cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                        }
                        if (comboMedBillType.SelectedIndex == 2)        // Medical Bill Type - Physical Therapy
                        {
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                            int nNumberOfPhysicalTherapy = 0;
                            short result = 0;
                            if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                            {
                                nNumberOfPhysicalTherapy = result;
                                cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                            }
                            else MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Alert");

                            cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                        }


                        cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCareTotal", 0);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCare", 0);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewOriginalPrescription", String.Empty);
                        cmdUpdateMedBill.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                        cmdUpdateMedBill.Parameters.AddWithValue("@IndividualId", IndividualId);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nAffectedRow = cmdUpdateMedBill.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        if (nAffectedRow == 1)
                        {
                            MessageBox.Show("The Medical Bill has been updated.", "Information");
                        }
                        else if (nAffectedRow == 0)
                        {
                            MessageBox.Show("The Medical Bill has not been updated.", "Error");
                        }

                        bIsModified = false;

                        tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                        tbCMMManager.SelectedIndex = 4;

                    }
                }
                else if (dlgResult == DialogResult.No)
                {
                    tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                    tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];
                    return;
                }
                else if (dlgResult == DialogResult.Cancel)
                {
                    return;
                }
            }
            else return;
        }

        //private void btnNewMedBill_Click(object sender, EventArgs e)
        //{

        //    int nRowSelected;
        //    String strCaseNameSelected = String.Empty;
        //    String strIndividualId = txtIndividualID.Text.Trim();
        //    String strPatientLastName = txtLastName.Text.Trim();
        //    String strPatientFirstName = txtFirstName.Text.Trim();
        //    String strPatientMiddleName = txtMiddleName.Text.Trim();
        //    String strDateOfBirth = txtDateOfBirth.Text.Trim();
        //    String strSSN = txtIndividualSSN.Text.Trim();
        //    String strStreetAddr = txtStreetAddress1.Text.Trim();
        //    String strCity = txtCity1.Text.Trim();
        //    String strState = txtState1.Text.Trim();
        //    String strZip = txtZip1.Text.Trim();


        //    if (gvCaseViewCaseHistory.Rows.Count > 0)
        //    {
        //        nRowSelected = gvCaseViewCaseHistory.CurrentCell.RowIndex;

        //        txtIndividualIDMedBill.Text = strIndividualId.Trim();

        //        if (strPatientMiddleName != String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;
        //        else txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName;

        //        txtMedBillDOB.Text = strDateOfBirth;
        //        txtMedBillSSN.Text = strSSN;
        //        txtMedBillAddress.Text = strStreetAddr + ", " + strCity + ", " + strState + " " + strZip;

        //        strCaseNameSelected = gvCaseViewCaseHistory["CaseName", nRowSelected].Value.ToString().Trim();
        //        strCaseIdSelected = strCaseNameSelected;
        //        strContactIdSelected = gvCaseViewCaseHistory["Individual_Id", nRowSelected].Value.ToString().Trim();

        //        strCaseIdForIllness = strCaseNameSelected;
        //        txtMedBill_CaseNo.Text = strCaseNameSelected;
        //        txtMedicalBillStatus.Text = "Pending Status";


        //        String strSqlQueryForCase = "select dbo.tbl_case.Case_Name, dbo.tbl_case.[NPF_Form], dbo.tbl_case.[NPF_Form_File_Name], dbo.tbl_case.[NPF_Receiv_Date], " +
        //                                    "dbo.tbl_case.[IB_Form], dbo.tbl_case.[IB_Form_File_Name], dbo.tbl_case.[IB_Receiv_Date], " +
        //                                    "dbo.tbl_case.[POP_Form], dbo.tbl_case.[POP_Form_File_Name], dbo.tbl_case.[POP_Receiv_Date], " +
        //                                    "dbo.tbl_case.[MedRec_Form], dbo.tbl_case.[MedRec_Form_File_Name], dbo.tbl_case.[MedRec_Receiv_Date], " +
        //                                    "dbo.tbl_case.[Unknown_Form], dbo.tbl_case.[Unknown_Form_File_Name], dbo.tbl_case.[Unknown_Receiv_Date] " +
        //                                    "from dbo.tbl_case where Case_Name = @CaseId";

        //        SqlCommand cmdQueryForDocumentReceivedDate = new SqlCommand(strSqlQueryForCase, connRN);
        //        cmdQueryForDocumentReceivedDate.CommandType = CommandType.Text;

        //        cmdQueryForDocumentReceivedDate.Parameters.AddWithValue("@CaseId", strCaseNameSelected);

        //        connRN.Open();
        //        SqlDataReader rdrDocsReceivedDate = cmdQueryForDocumentReceivedDate.ExecuteReader();

        //        if (rdrDocsReceivedDate.HasRows)
        //        {
        //            rdrDocsReceivedDate.Read();

        //            if (rdrDocsReceivedDate.GetBoolean(1) == true)
        //            {
        //                chkMedBillNPFReceived.Checked = true;
        //                dtpMedBillNPF.Format = DateTimePickerFormat.Short;
        //                dtpMedBillNPF.Value = rdrDocsReceivedDate.GetDateTime(3);
        //                btnViewNPF.Enabled = true;
        //            }
        //            else
        //            {
        //                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
        //                dtpMedBillNPF.CustomFormat = " ";
        //                btnViewNPF.Enabled = false;
        //            }

        //            chkMedBillNPFReceived.Enabled = false;

        //            if (rdrDocsReceivedDate.GetBoolean(4) == true)
        //            {
        //                chkMedBill_IBReceived.Checked = true;
        //                dtpMedBill_IB.Format = DateTimePickerFormat.Short;
        //                dtpMedBill_IB.Value = rdrDocsReceivedDate.GetDateTime(6);
        //                btnViewIB.Enabled = true;
        //            }
        //            else
        //            {
        //                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
        //                dtpMedBill_IB.CustomFormat = " ";
        //                btnViewIB.Enabled = false;
        //            }

        //            chkMedBill_IBReceived.Enabled = false;

        //            if (rdrDocsReceivedDate.GetBoolean(7) == true)
        //            {
        //                chkMedBillPOPReceived.Checked = true;
        //                dtpMedBillPOP.Format = DateTimePickerFormat.Short;
        //                dtpMedBillPOP.Value = rdrDocsReceivedDate.GetDateTime(9);
        //                btnViewPoP.Enabled = true;
        //            }
        //            else
        //            {
        //                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
        //                dtpMedBillPOP.CustomFormat = " ";
        //                btnViewPoP.Enabled = false;
        //            }

        //            chkMedBillPOPReceived.Enabled = false;

        //            if (rdrDocsReceivedDate.GetBoolean(10) == true)
        //            {
        //                chkMedRecordReceived.Checked = true;
        //                dtpMedBillMedRecord.Format = DateTimePickerFormat.Short;
        //                dtpMedBillMedRecord.Value = rdrDocsReceivedDate.GetDateTime(12);
        //                btnViewMedRecord.Enabled = true;
        //            }
        //            else
        //            {
        //                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
        //                dtpMedBillMedRecord.CustomFormat = " ";
        //                btnViewMedRecord.Enabled = false;
        //            }

        //            chkMedRecordReceived.Enabled = false;

        //            if (rdrDocsReceivedDate.GetBoolean(13) == true)
        //            {
        //                chkOtherDocReceived.Checked = true;
        //                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Short;
        //                dtpMedBillOtherDoc.Value = rdrDocsReceivedDate.GetDateTime(15);
        //                btnViewOtherDoc.Enabled = true;
        //            }
        //            else
        //            {
        //                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
        //                dtpMedBillOtherDoc.CustomFormat = " ";
        //                btnViewOtherDoc.Enabled = false;
        //            }

        //            chkOtherDocReceived.Enabled = false;

        //        }
        //        connRN.Close();

        //        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //        String strQueryForICD10Codes = "select dbo.[ICD10 Code].ID, dbo.[ICD10 Code].ICD10_CODE__C, dbo.[ICD10 Code].Name from [dbo].[ICD10 Code]";

        //        SqlCommand cmdQueryForICD10Codes = new SqlCommand(strQueryForICD10Codes, connSalesforce);

        //        cmdQueryForICD10Codes.CommandType = CommandType.Text;
        //        cmdQueryForICD10Codes.CommandText = strQueryForICD10Codes;

        //        connSalesforce.Open();
        //        SqlDataReader rdrICD10Codes = cmdQueryForICD10Codes.ExecuteReader();


        //        lstICD10CodeInfo.Clear();

        //        if (rdrICD10Codes.HasRows)
        //        {
        //            while (rdrICD10Codes.Read())
        //            {
        //                lstICD10CodeInfo.Add(new ICD10CodeInfo { Id = rdrICD10Codes.GetString(0), ICD10Code = rdrICD10Codes.GetString(1), Name = rdrICD10Codes.GetString(2) });
        //            }
        //        }

        //        connSalesforce.Close();

        //        var srcICD10Codes = new AutoCompleteStringCollection();

        //        for (int i = 0; i < lstICD10CodeInfo.Count; i++)
        //        {
        //            srcICD10Codes.Add(lstICD10CodeInfo[i].ICD10Code);
        //        }

        //        txtMedBill_ICD10Code.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        //        txtMedBill_ICD10Code.AutoCompleteSource = AutoCompleteSource.CustomSource;
        //        txtMedBill_ICD10Code.AutoCompleteCustomSource = srcICD10Codes;

        //        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //        String strSqlQueryForMaxMedBillNo = "select max(dbo.tbl_medbill.BillNo) from dbo.tbl_medbill";

        //        SqlCommand cmdQueryForMaxBillNo = new SqlCommand(strSqlQueryForMaxMedBillNo, connRN);
        //        cmdQueryForMaxBillNo.CommandType = CommandType.Text;

        //        connRN.Open();
        //        String strMaxMedBillNo = cmdQueryForMaxBillNo.ExecuteScalar().ToString();
        //        connRN.Close();
        //        String strNewMedBillNo = String.Empty;


        //        if (strMaxMedBillNo != String.Empty)
        //        {
        //            int nNewMedBillNo = Int32.Parse(strMaxMedBillNo.Substring(8));
        //            nNewMedBillNo++;
        //            int nLeadingZero = 0;
        //            while ((nNewMedBillNo.ToString().Length + nLeadingZero) < 7) nLeadingZero++;
        //            strNewMedBillNo = "MEDBILL-";
        //            for (int i = 0; i < nLeadingZero; i++) strNewMedBillNo += '0';

        //            strNewMedBillNo += nNewMedBillNo.ToString();
        //        }


        //        txtMedBillNo.Text = strNewMedBillNo;


        //        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //        String strSqlQueryForMedicalProvider = "select dbo.tbl_MedicalProvider.ID, dbo.tbl_MedicalProvider.Name, dbo.tbl_MedicalProvider.Type from dbo.tbl_MedicalProvider";

        //        SqlCommand cmdQueryForMedicalProvider = new SqlCommand(strSqlQueryForMedicalProvider, connRN);
        //        cmdQueryForMedicalProvider.CommandType = CommandType.Text;

        //        connRN.Open();

        //        SqlDataReader rdrMedicalProvider = cmdQueryForMedicalProvider.ExecuteReader();

        //        lstMedicalProvider.Clear();
        //        if (rdrMedicalProvider.HasRows)
        //        {
        //            while (rdrMedicalProvider.Read())
        //            {
        //                MedicalProviderInfo info = new MedicalProviderInfo();

        //                if (!rdrMedicalProvider.IsDBNull(0)) info.ID = rdrMedicalProvider.GetString(0);
        //                if (!rdrMedicalProvider.IsDBNull(1)) info.Name = rdrMedicalProvider.GetString(1);
        //                if (!rdrMedicalProvider.IsDBNull(2)) info.Type = rdrMedicalProvider.GetString(2);

        //                lstMedicalProvider.Add(info);
        //            }
        //        }

        //        connRN.Close();

        //        var srcMedicalProvider = new AutoCompleteStringCollection();

        //        for (int i = 0; i < lstMedicalProvider.Count; i++)
        //        {
        //            srcMedicalProvider.Add(lstMedicalProvider[i].Name);
        //        }

        //        txtMedicalProvider.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        //        txtMedicalProvider.AutoCompleteSource = AutoCompleteSource.CustomSource;
        //        txtMedicalProvider.AutoCompleteCustomSource = srcMedicalProvider;


        //        tbCMMManager.TabPages.Insert(3, tbpgMedicalBill);
        //        tbCMMManager.SelectedIndex = 3;
        //    }
        //}

        private void btnNewMedBill_Case_Click(object sender, EventArgs e)
        {
            if (!tbCMMManager.TabPages.Contains(tbpgMedicalBill))
            {
                MedBillStart = PrevTabPage;
                medbillMode = MedBillMode.AddNew;

                //gvCaseViewCaseHistory
                int nRowSelected;

                String strCaseNameSelected = String.Empty;
                String strPatientLastName = txtLastName.Text.Trim();
                String strPatientFirstName = txtFirstName.Text.Trim();
                String strPatientMiddleName = txtMiddleName.Text.Trim();
                String strDateOfBirth = dtpBirthDate.Value.ToString("MM/dd/yyyy");
                String strSSN = txtIndividualSSN.Text.Trim();
                String strStreetAddr = txtStreetAddress1.Text.Trim();
                String strCity = txtCity1.Text.Trim();
                String strState = txtState1.Text.Trim();
                String strZip = txtZip1.Text.Trim();

                if (PrevTabPage == TabPage.Individual)
                {
                    if (gvProcessingCaseNo.Rows.Count > 0)
                    {
                        InitializeMedBillTabOnNewMedBill();

                        //nRowSelected = gvProcessingCaseNo.CurrentCell.RowIndex;

                        txtIndividualIDMedBill.Text = strIndividualId.Trim();

                        if (strPatientMiddleName != String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;
                        else txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName;

                        txtMedBillDOB.Text = strDateOfBirth;
                        txtMedBillSSN.Text = strSSN;
                        txtMedBillAddress.Text = strStreetAddr + ", " + strCity + ", " + strState + " " + strZip;

                        //strCaseNameSelected = gvProcessingCaseNo["CaseIdForIndividual", nRowSelected].Value.ToString().Trim();
                        strCaseNameSelected = txtCaseName.Text.Trim();
                        strCaseIdSelected = strCaseNameSelected;
                        strContactIdSelected = strIndividualId;

                        strCaseIdForIllness = strCaseNameSelected;
                        txtMedBill_CaseNo.Text = strCaseNameSelected;
                        //txtMedicalBillStatus.Text = "Pending Status";

                        String strSqlQueryForCase = "select dbo.tbl_case.Case_Name, dbo.tbl_case.[NPF_Form], dbo.tbl_case.[NPF_Form_File_Name], dbo.tbl_case.[NPF_Receiv_Date], " +
                                "dbo.tbl_case.[IB_Form], dbo.tbl_case.[IB_Form_File_Name], dbo.tbl_case.[IB_Receiv_Date], " +
                                "dbo.tbl_case.[POP_Form], dbo.tbl_case.[POP_Form_File_Name], dbo.tbl_case.[POP_Receiv_Date], " +
                                "dbo.tbl_case.[MedRec_Form], dbo.tbl_case.[MedRec_Form_File_Name], dbo.tbl_case.[MedRec_Receiv_Date], " +
                                "dbo.tbl_case.[Unknown_Form], dbo.tbl_case.[Unknown_Form_File_Name], dbo.tbl_case.[Unknown_Receiv_Date] " +
                                "from dbo.tbl_case where Case_Name = @CaseId";

                        SqlCommand cmdQueryForDocumentReceivedDate = new SqlCommand(strSqlQueryForCase, connRN);
                        cmdQueryForDocumentReceivedDate.CommandType = CommandType.Text;

                        cmdQueryForDocumentReceivedDate.Parameters.AddWithValue("@CaseId", strCaseNameSelected);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        SqlDataReader rdrDocsReceivedDate = cmdQueryForDocumentReceivedDate.ExecuteReader();

                        if (rdrDocsReceivedDate.HasRows)
                        {
                            rdrDocsReceivedDate.Read();

                            if (rdrDocsReceivedDate.GetBoolean(1) == true)
                            {
                                chkMedBillNPFReceived.Checked = true;
                                dtpMedBillNPF.Format = DateTimePickerFormat.Short;
                                dtpMedBillNPF.Value = rdrDocsReceivedDate.GetDateTime(3);
                                btnViewNPF.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
                                dtpMedBillNPF.CustomFormat = " ";
                                btnViewNPF.Enabled = false;
                            }

                            chkMedBillNPFReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(4) == true)
                            {
                                chkMedBill_IBReceived.Checked = true;
                                dtpMedBill_IB.Format = DateTimePickerFormat.Short;
                                dtpMedBill_IB.Value = rdrDocsReceivedDate.GetDateTime(6);
                                btnViewIB.Enabled = true;
                            }
                            else
                            {
                                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
                                dtpMedBill_IB.CustomFormat = " ";
                                btnViewIB.Enabled = false;
                            }

                            chkMedBill_IBReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(7) == true)
                            {
                                chkMedBillPOPReceived.Checked = true;
                                dtpMedBillPOP.Format = DateTimePickerFormat.Short;
                                dtpMedBillPOP.Value = rdrDocsReceivedDate.GetDateTime(9);
                                btnViewPoP.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
                                dtpMedBillPOP.CustomFormat = " ";
                                btnViewPoP.Enabled = false;
                            }

                            chkMedBillPOPReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(10) == true)
                            {
                                chkMedRecordReceived.Checked = true;
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Short;
                                dtpMedBillMedRecord.Value = rdrDocsReceivedDate.GetDateTime(12);
                                btnViewMedRecord.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
                                dtpMedBillMedRecord.CustomFormat = " ";
                                btnViewMedRecord.Enabled = false;
                            }

                            chkMedRecordReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(13) == true)
                            {
                                chkOtherDocReceived.Checked = true;
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Short;
                                dtpMedBillOtherDoc.Value = rdrDocsReceivedDate.GetDateTime(15);
                                btnViewOtherDoc.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
                                dtpMedBillOtherDoc.CustomFormat = " ";
                                btnViewOtherDoc.Enabled = false;
                            }

                            chkOtherDocReceived.Enabled = false;

                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        String strQueryForICD10Codes = "select dbo.[ICD10 Code].ID, dbo.[ICD10 Code].ICD10_CODE__C, dbo.[ICD10 Code].Name from [dbo].[ICD10 Code]";

                        SqlCommand cmdQueryForICD10Codes = new SqlCommand(strQueryForICD10Codes, connSalesforce);

                        cmdQueryForICD10Codes.CommandType = CommandType.Text;
                        cmdQueryForICD10Codes.CommandText = strQueryForICD10Codes;

                        //if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                        if (connSalesforce.State == ConnectionState.Open)
                        {
                            connSalesforce.Close();
                            connSalesforce.Open();
                        }
                        else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                        SqlDataReader rdrICD10Codes = cmdQueryForICD10Codes.ExecuteReader();

                        lstICD10CodeInfo.Clear();
                        if (rdrICD10Codes.HasRows)
                        {
                            while (rdrICD10Codes.Read())
                            {
                                lstICD10CodeInfo.Add(new ICD10CodeInfo { Id = rdrICD10Codes.GetString(0), ICD10Code = rdrICD10Codes.GetString(1), Name = rdrICD10Codes.GetString(2) });
                            }
                        }

                        if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                        var srcICD10Codes = new AutoCompleteStringCollection();

                        for (int i = 0; i < lstICD10CodeInfo.Count; i++)
                        {
                            srcICD10Codes.Add(lstICD10CodeInfo[i].ICD10Code);
                        }

                        txtMedBill_ICD10Code.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        txtMedBill_ICD10Code.AutoCompleteSource = AutoCompleteSource.CustomSource;
                        txtMedBill_ICD10Code.AutoCompleteCustomSource = srcICD10Codes;

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        String strSqlQueryForMaxMedBillNo = "select max(dbo.tbl_medbill.BillNo) from dbo.tbl_medbill";

                        SqlCommand cmdQueryForMaxBillNo = new SqlCommand(strSqlQueryForMaxMedBillNo, connRN);
                        cmdQueryForMaxBillNo.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        //String strMaxMedBillNo = cmdQueryForMaxBillNo.ExecuteScalar().ToString();
                        Object objMaxMedBillNo = cmdQueryForMaxBillNo.ExecuteScalar();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        String strMaxMedBillNo = String.Empty;
                        if (objMaxMedBillNo != null) strMaxMedBillNo = objMaxMedBillNo.ToString();

                        String strNewMedBillNo = String.Empty;
                        if (strMaxMedBillNo != String.Empty)
                        {
                            int nNewMedBillNo = Int32.Parse(strMaxMedBillNo.Substring(8));
                            nNewMedBillNo++;
                            int nLeadingZero = 0;
                            while ((nNewMedBillNo.ToString().Length + nLeadingZero) < 7) nLeadingZero++;
                            strNewMedBillNo = "MEDBILL-";
                            for (int i = 0; i < nLeadingZero; i++) strNewMedBillNo += '0';

                            strNewMedBillNo += nNewMedBillNo.ToString();
                        }
                        else strNewMedBillNo = "MEDBILL - 0150000";
                        txtMedBillNo.Text = strNewMedBillNo;

                        // Populate Medical Bill Type combo box

                        String strSqlQueryForMedBillTypes = "select [dbo].[tbl_medbill_type].[MedBillTypeName] from [dbo].[tbl_medbill_type]";

                        SqlCommand cmdQueryForMedBillTypes = new SqlCommand(strSqlQueryForMedBillTypes, connRN);
                        cmdQueryForMedBillTypes.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlDataReader rdrMedBillTypes = cmdQueryForMedBillTypes.ExecuteReader();
                        comboMedBillType.Items.Clear();
                        if (rdrMedBillTypes.HasRows)
                        {
                            while (rdrMedBillTypes.Read())
                            {
                                if (!rdrMedBillTypes.IsDBNull(0)) comboMedBillType.Items.Add(rdrMedBillTypes.GetString(0));
                            }
                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();
                        comboMedBillType.SelectedIndex = (int)MedBillType.MedicalBill - 1;

                        // Populate Pending Reason
                        comboPendingReason.Items.Clear();
                        if (dicPendingReason.Count > 0)
                        {
                            for (int i = 0; i < dicPendingReason.Count; i++)
                            {
                                comboPendingReason.Items.Add(dicPendingReason[i]);
                            }
                            comboPendingReason.SelectedIndex = 0;
                        }

                        // Populate Ineligible Reason
                        comboIneligibleReason.Items.Clear();
                        if (dicIneligibleReason.Count > 0)
                        {
                            for (int i = 0; i < dicIneligibleReason.Count; i++)
                            {
                                comboIneligibleReason.Items.Add(dicIneligibleReason[i]);
                            }
                            comboIneligibleReason.SelectedIndex = 0;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        String strSqlQueryForMedicalProvider = "select dbo.tbl_MedicalProvider.ID, dbo.tbl_MedicalProvider.Name, dbo.tbl_MedicalProvider.Type from dbo.tbl_MedicalProvider";

                        SqlCommand cmdQueryForMedicalProvider = new SqlCommand(strSqlQueryForMedicalProvider, connRN);
                        cmdQueryForMedicalProvider.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlDataReader rdrMedicalProvider = cmdQueryForMedicalProvider.ExecuteReader();

                        lstMedicalProvider.Clear();
                        if (rdrMedicalProvider.HasRows)
                        {
                            while (rdrMedicalProvider.Read())
                            {
                                MedicalProviderInfo info = new MedicalProviderInfo();

                                if (!rdrMedicalProvider.IsDBNull(0)) info.ID = rdrMedicalProvider.GetString(0);
                                if (!rdrMedicalProvider.IsDBNull(1)) info.Name = rdrMedicalProvider.GetString(1);
                                if (!rdrMedicalProvider.IsDBNull(2)) info.Type = rdrMedicalProvider.GetString(2);

                                lstMedicalProvider.Add(info);
                            }
                        }

                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        var srcMedicalProvider = new AutoCompleteStringCollection();

                        for (int i = 0; i < lstMedicalProvider.Count; i++)
                        {
                            srcMedicalProvider.Add(lstMedicalProvider[i].Name);
                        }


                        // Med Bill fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        rbOutpatient.Checked = false;
                        rbInpatient.Checked = false;
                        txtMedBillNote.Text = String.Empty;

                        // Prescription fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        txtPrescriptionName.Text = String.Empty;
                        txtNumberOfMedication.Text = String.Empty;
                        txtPrescriptionDescription.Text = String.Empty;
                        txtPrescriptionNote.Text = String.Empty;

                        // Physical Therapy fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        txtNumPhysicalTherapy.Text = String.Empty;
                        txtPhysicalTherapyRxNote.Text = String.Empty;

                        //// Etc group
                        //txtMedBillAccountNoAtProvider.Text = String.Empty;
                        //txtMedProviderPhoneNo.Text = String.Empty;
                        //txtProviderContactPerson.Text = String.Empty;

                        //tbCMMManager.TabPages.Remove(tbpgCreateCase);
                        tbCMMManager.TabPages.Insert(5, tbpgMedicalBill);
                        tbCMMManager.SelectedIndex = 5;

                    }
                    else
                    {
                        MessageBox.Show("Please create a case first.", "Alert");
                    }
                }
                //else if (PrevTabPage == TabPage.CaseView)
                else
                {
                    if (gvCaseViewCaseHistory.Rows.Count > 0)
                    {

                        InitializeMedBillTabOnNewMedBill();
                        //nRowSelected = gvCaseViewCaseHistory.CurrentCell.RowIndex;

                        txtIndividualIDMedBill.Text = strIndividualId.Trim();

                        if (strPatientMiddleName != String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;
                        else txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName;

                        txtMedBillDOB.Text = strDateOfBirth;
                        txtMedBillSSN.Text = strSSN;
                        txtMedBillAddress.Text = strStreetAddr + ", " + strCity + ", " + strState + " " + strZip;

                        //strCaseNameSelected = gvCaseViewCaseHistory["CaseName", nRowSelected].Value.ToString().Trim();
                        strCaseNameSelected = txtCaseName.Text.Trim();
                        strCaseIdSelected = strCaseNameSelected;
                        //strContactIdSelected = gvCaseViewCaseHistory["Individual_Id", nRowSelected].Value.ToString().Trim();
                        strContactIdSelected = strIndividualId;

                        strCaseIdForIllness = strCaseNameSelected;
                        txtMedBill_CaseNo.Text = strCaseNameSelected;
                        //txtMedicalBillStatus.Text = "Pending Status";


                        String strSqlQueryForCase = "select dbo.tbl_case.Case_Name, dbo.tbl_case.[NPF_Form], dbo.tbl_case.[NPF_Form_File_Name], dbo.tbl_case.[NPF_Receiv_Date], " +
                                                    "dbo.tbl_case.[IB_Form], dbo.tbl_case.[IB_Form_File_Name], dbo.tbl_case.[IB_Receiv_Date], " +
                                                    "dbo.tbl_case.[POP_Form], dbo.tbl_case.[POP_Form_File_Name], dbo.tbl_case.[POP_Receiv_Date], " +
                                                    "dbo.tbl_case.[MedRec_Form], dbo.tbl_case.[MedRec_Form_File_Name], dbo.tbl_case.[MedRec_Receiv_Date], " +
                                                    "dbo.tbl_case.[Unknown_Form], dbo.tbl_case.[Unknown_Form_File_Name], dbo.tbl_case.[Unknown_Receiv_Date] " +
                                                    "from dbo.tbl_case where Case_Name = @CaseId";

                        SqlCommand cmdQueryForDocumentReceivedDate = new SqlCommand(strSqlQueryForCase, connRN);
                        cmdQueryForDocumentReceivedDate.CommandType = CommandType.Text;

                        cmdQueryForDocumentReceivedDate.Parameters.AddWithValue("@CaseId", strCaseNameSelected);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        SqlDataReader rdrDocsReceivedDate = cmdQueryForDocumentReceivedDate.ExecuteReader();

                        if (rdrDocsReceivedDate.HasRows)
                        {
                            rdrDocsReceivedDate.Read();

                            if (rdrDocsReceivedDate.GetBoolean(1) == true)
                            {
                                chkMedBillNPFReceived.Checked = true;
                                dtpMedBillNPF.Format = DateTimePickerFormat.Short;
                                dtpMedBillNPF.Value = rdrDocsReceivedDate.GetDateTime(3);
                                btnViewNPF.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
                                dtpMedBillNPF.CustomFormat = " ";
                                btnViewNPF.Enabled = false;
                            }

                            chkMedBillNPFReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(4) == true)
                            {
                                chkMedBill_IBReceived.Checked = true;
                                dtpMedBill_IB.Format = DateTimePickerFormat.Short;
                                dtpMedBill_IB.Value = rdrDocsReceivedDate.GetDateTime(6);
                                btnViewIB.Enabled = true;
                            }
                            else
                            {
                                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
                                dtpMedBill_IB.CustomFormat = " ";
                                btnViewIB.Enabled = false;
                            }

                            chkMedBill_IBReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(7) == true)
                            {
                                chkMedBillPOPReceived.Checked = true;
                                dtpMedBillPOP.Format = DateTimePickerFormat.Short;
                                dtpMedBillPOP.Value = rdrDocsReceivedDate.GetDateTime(9);
                                btnViewPoP.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
                                dtpMedBillPOP.CustomFormat = " ";
                                btnViewPoP.Enabled = false;
                            }

                            chkMedBillPOPReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(10) == true)
                            {
                                chkMedRecordReceived.Checked = true;
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Short;
                                dtpMedBillMedRecord.Value = rdrDocsReceivedDate.GetDateTime(12);
                                btnViewMedRecord.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
                                dtpMedBillMedRecord.CustomFormat = " ";
                                btnViewMedRecord.Enabled = false;
                            }

                            chkMedRecordReceived.Enabled = false;

                            if (rdrDocsReceivedDate.GetBoolean(13) == true)
                            {
                                chkOtherDocReceived.Checked = true;
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Short;
                                dtpMedBillOtherDoc.Value = rdrDocsReceivedDate.GetDateTime(15);
                                btnViewOtherDoc.Enabled = true;
                            }
                            else
                            {
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
                                dtpMedBillOtherDoc.CustomFormat = " ";
                                btnViewOtherDoc.Enabled = false;
                            }

                            chkOtherDocReceived.Enabled = false;

                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();


                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        String strQueryForICD10Codes = "select dbo.[ICD10 Code].ID, dbo.[ICD10 Code].ICD10_CODE__C, dbo.[ICD10 Code].Name from [dbo].[ICD10 Code]";

                        SqlCommand cmdQueryForICD10Codes = new SqlCommand(strQueryForICD10Codes, connSalesforce);

                        cmdQueryForICD10Codes.CommandType = CommandType.Text;
                        cmdQueryForICD10Codes.CommandText = strQueryForICD10Codes;

                        //if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                        if (connSalesforce.State == ConnectionState.Open)
                        {
                            connSalesforce.Close();
                            connSalesforce.Open();
                        }
                        else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                        SqlDataReader rdrICD10Codes = cmdQueryForICD10Codes.ExecuteReader();

                        lstICD10CodeInfo.Clear();
                        if (rdrICD10Codes.HasRows)
                        {
                            while (rdrICD10Codes.Read())
                            {
                                lstICD10CodeInfo.Add(new ICD10CodeInfo { Id = rdrICD10Codes.GetString(0), ICD10Code = rdrICD10Codes.GetString(1), Name = rdrICD10Codes.GetString(2) });
                            }
                        }

                        if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                        var srcICD10Codes = new AutoCompleteStringCollection();

                        for (int i = 0; i < lstICD10CodeInfo.Count; i++)
                        {
                            srcICD10Codes.Add(lstICD10CodeInfo[i].ICD10Code);
                        }

                        txtMedBill_ICD10Code.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        txtMedBill_ICD10Code.AutoCompleteSource = AutoCompleteSource.CustomSource;
                        txtMedBill_ICD10Code.AutoCompleteCustomSource = srcICD10Codes;

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        String strSqlQueryForMaxMedBillNo = "select max(dbo.tbl_medbill.BillNo) from dbo.tbl_medbill";

                        SqlCommand cmdQueryForMaxBillNo = new SqlCommand(strSqlQueryForMaxMedBillNo, connRN);
                        cmdQueryForMaxBillNo.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        //String strMaxMedBillNo = cmdQueryForMaxBillNo.ExecuteScalar().ToString();
                        Object objMaxMedBillNo = cmdQueryForMaxBillNo.ExecuteScalar();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        String strMaxMedBillNo = String.Empty;

                        if (objMaxMedBillNo != null) strMaxMedBillNo = objMaxMedBillNo.ToString();

                        String strNewMedBillNo = String.Empty;

                        if (strMaxMedBillNo != String.Empty)
                        {
                            int nNewMedBillNo = Int32.Parse(strMaxMedBillNo.Substring(8));
                            nNewMedBillNo++;
                            int nLeadingZero = 0;
                            while ((nNewMedBillNo.ToString().Length + nLeadingZero) < 7) nLeadingZero++;
                            strNewMedBillNo = "MEDBILL-";
                            for (int i = 0; i < nLeadingZero; i++) strNewMedBillNo += '0';

                            strNewMedBillNo += nNewMedBillNo.ToString();
                        }
                        else strNewMedBillNo = "MEDBILL - 0150000";
                        txtMedBillNo.Text = strNewMedBillNo;
                        
                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Populate the medical bill types
                        /// 
                        //String strSqlQueryForMedBillTypes = "select [dbo].[tbl_medbill_type].[MedBillTypeId], [dbo].[tbl_medbill_type].[MedBillTypeName] from [dbo].[tbl_medbill_type]";
                        String strSqlQueryForMedBillTypes = "select [dbo].[tbl_medbill_type].[MedBillTypeName] from [dbo].[tbl_medbill_type]";

                        SqlCommand cmdQueryForMedBillTypes = new SqlCommand(strSqlQueryForMedBillTypes, connRN);
                        cmdQueryForMedBillTypes.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        SqlDataReader rdrMedBillTypes = cmdQueryForMedBillTypes.ExecuteReader();
                        comboMedBillType.Items.Clear();

                        if (rdrMedBillTypes.HasRows)
                        {
                            while (rdrMedBillTypes.Read())
                            {
                                if (!rdrMedBillTypes.IsDBNull(0)) comboMedBillType.Items.Add(rdrMedBillTypes.GetString(0));
                            }
                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();
                        comboMedBillType.SelectedIndex = (int)MedBillType.MedicalBill - 1;

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Populate the medical bill status
                        /// 
                        comboMedBillStatus.Items.Clear();
                        if (dicMedBillStatus.Count > 0)
                        {
                            for (int i = 0; i < dicMedBillStatus.Count; i++)
                            {
                                comboMedBillStatus.Items.Add(dicMedBillStatus[i]);
                            }
                            comboMedBillStatus.SelectedIndex = 0;
                        }


                        // Populate Pending Reason
                        comboPendingReason.Items.Clear();
                        if (dicPendingReason.Count > 0)
                        {
                            for (int i = 0; i < dicPendingReason.Count; i++)
                            {
                                comboPendingReason.Items.Add(dicPendingReason[i]);
                            }
                            comboPendingReason.SelectedIndex = 0;
                        }

                        // Populate Ineligible Reason
                        comboIneligibleReason.Items.Clear();
                        if (dicIneligibleReason.Count > 0)
                        {
                            for (int i = 0; i < dicIneligibleReason.Count; i++)
                            {
                                comboIneligibleReason.Items.Add(dicIneligibleReason[i]);
                            }
                            comboIneligibleReason.SelectedIndex = 0;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        String strSqlQueryForMedicalProvider = "select dbo.tbl_MedicalProvider.ID, dbo.tbl_MedicalProvider.Name, dbo.tbl_MedicalProvider.Type from dbo.tbl_MedicalProvider";

                        SqlCommand cmdQueryForMedicalProvider = new SqlCommand(strSqlQueryForMedicalProvider, connRN);
                        cmdQueryForMedicalProvider.CommandType = CommandType.Text;

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlDataReader rdrMedicalProvider = cmdQueryForMedicalProvider.ExecuteReader();

                        lstMedicalProvider.Clear();
                        if (rdrMedicalProvider.HasRows)
                        {
                            while (rdrMedicalProvider.Read())
                            {
                                MedicalProviderInfo info = new MedicalProviderInfo();

                                if (!rdrMedicalProvider.IsDBNull(0)) info.ID = rdrMedicalProvider.GetString(0);
                                if (!rdrMedicalProvider.IsDBNull(1)) info.Name = rdrMedicalProvider.GetString(1);
                                if (!rdrMedicalProvider.IsDBNull(2)) info.Type = rdrMedicalProvider.GetString(2);

                                lstMedicalProvider.Add(info);
                            }
                        }

                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        var srcMedicalProvider = new AutoCompleteStringCollection();

                        for (int i = 0; i < lstMedicalProvider.Count; i++)
                        {
                            srcMedicalProvider.Add(lstMedicalProvider[i].Name);
                        }

                        txtMedicalProvider.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        txtMedicalProvider.AutoCompleteSource = AutoCompleteSource.CustomSource;
                        txtMedicalProvider.AutoCompleteCustomSource = srcMedicalProvider;

                        // Med Bill fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        rbOutpatient.Checked = false;
                        rbInpatient.Checked = false;
                        txtMedBillNote.Text = String.Empty;

                        // Prescription fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        txtPrescriptionName.Text = String.Empty;
                        txtNumberOfMedication.Text = String.Empty;
                        txtPrescriptionDescription.Text = String.Empty;
                        txtPrescriptionNote.Text = String.Empty;

                        // Physical Therapy fields initialization
                        txtMedBillAmount.Text = String.Empty;
                        txtBalance.Text = String.Empty;
                        txtNumPhysicalTherapy.Text = String.Empty;
                        txtPhysicalTherapyRxNote.Text = String.Empty;

                        //// Etc group
                        //txtMedBillAccountNoAtProvider.Text = String.Empty;
                        //txtMedProviderPhoneNo.Text = String.Empty;
                        //txtProviderContactPerson.Text = String.Empty;

                        //tbCMMManager.TabPages.Remove(tbpgCreateCase);
                        tbCMMManager.TabPages.Insert(5, tbpgMedicalBill);
                        tbCMMManager.SelectedIndex = 5;
                    }
                    else
                    {
                        MessageBox.Show("Please create a case first", "Alert");
                    }
                }


                /////////////////////////////////////////////////////////////////////////////////////////////////////
                btnAddNewSettlement.Enabled = false;
                btnSaveSettlement.Enabled = false;
                //btnEditSettlement.Enabled = false;
                btnDeleteSettlement.Enabled = false;

            }
            else MessageBox.Show("Medical Bill page is already open", "Alert");
        }

        private void InitializeMedBillTabOnNewMedBill()
        {
            txtMedBill_Illness.Text = String.Empty;
            txtMedBill_Incident.Text = String.Empty;
            txtIncdProgram.Text = String.Empty;

            txtMedBill_ICD10Code.Text = String.Empty;
            txtMedBillDiseaseName.Text = String.Empty;

            //txtMedicalBillStatus.Text = "Pending Status";
            txtMedBillAmount.Text = String.Empty;
            txtNumPhysicalTherapy.Text = String.Empty;
            txtNumberOfMedication.Text = String.Empty;
            txtPrescriptionDescription.Text = String.Empty;
            txtPrescriptionName.Text = String.Empty;
            txtMedicalProvider.Text = String.Empty;
            rbInpatient.Checked = false;
            rbOutpatient.Checked = false;

            // Etc group
            txtMedBillAccountNoAtProvider.Text = String.Empty;
            txtMedProviderPhoneNo.Text = String.Empty;
            txtProviderContactPerson.Text = String.Empty;

            dtpBillDate.Text = String.Empty;
            dtpDueDate.Text = String.Empty;

            gvSettlementsInMedBill.Rows.Clear();
        }

        private void tbCMMManager_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardRNManager"]) ||
                (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardRNStaff"]) ||
                (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardNPManager"]) ||
                (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardNPStaff"]) ||
                (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardFDManager"]) ||
                (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgDashboardFDStaff"]))
            {
                BeforePrevTabPage = PrevTabPage;
                PrevTabPage = CurrentTabPage;
                CurrentTabPage = TabPage.DashBoard;
            }

            if (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgIndividual"])
            {
                BeforePrevTabPage = PrevTabPage;
                PrevTabPage = CurrentTabPage;
                CurrentTabPage = TabPage.Individual;
            }

            if (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgCaseView"])
            {
                BeforePrevTabPage = PrevTabPage;
                PrevTabPage = CurrentTabPage;
                CurrentTabPage = TabPage.CaseView;
            }

            if (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgCreateCase"])
            {
                BeforePrevTabPage = PrevTabPage;
                PrevTabPage = CurrentTabPage;
                CurrentTabPage = TabPage.Case;
            }

            if (tbCMMManager.SelectedTab == tbCMMManager.TabPages["tbpgMedicalBill"])
            {
                BeforePrevTabPage = PrevTabPage;
                PrevTabPage = CurrentTabPage;
                CurrentTabPage = TabPage.MedBill;
            }
        }

        //private void gvSettlementsInMedBill_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        //{

        //}

        //private void gvSettlementsInMedBill_RowValidated(object sender, DataGridViewCellEventArgs e)
        //{
        //    MessageBox.Show("Row has been validated");
        //}

        private void btnAddNewSettlement_Click(object sender, EventArgs e)
        {
            settlementMode = SettlementMode.AddNew;

            //gvSettlementsInMedBill.Rows.Add();
            //gvSettlementsInMedBill[0, 0].Value = true;

            String SettlementName = "STLM-";

            int nFirstSettlementNo = 300000;

            String strSqlQueryForMaxSettlementName = "select max([dbo].[tbl_settlement].[Name]) from [dbo].[tbl_settlement]";

            SqlCommand cmdQueryForMaxSettlement = new SqlCommand(strSqlQueryForMaxSettlementName, connRN);
            cmdQueryForMaxSettlement.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();

            //String MaxSettlementName = cmdQueryForMaxSettlement.ExecuteScalar().ToString();
            Object objMaxSettlementName = cmdQueryForMaxSettlement.ExecuteScalar();
            if (connRN.State == ConnectionState.Open) connRN.Close();

            String MaxSettlementName = String.Empty;

            if (objMaxSettlementName != null) MaxSettlementName = objMaxSettlementName.ToString();

            if (MaxSettlementName == String.Empty) SettlementName += nFirstSettlementNo.ToString();
            else
            {
                int NextSettlementNo = Int32.Parse(MaxSettlementName.Substring(5));
                NextSettlementNo++;
                SettlementName += NextSettlementNo.ToString();
            }

            if (gvSettlementsInMedBill.Rows.Count == 0)
            {
                gvSettlementsInMedBill.Rows.Add();
                gvSettlementsInMedBill["Selected", 0].Value = true;
                gvSettlementsInMedBill["SettlementName", 0].Value = SettlementName;

                // Populate settlement type
                DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
                for (int i = 0; i < lstSettlementType.Count; i++)
                {
                    comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                }
                gvSettlementsInMedBill[2, 0] = comboCellSettlementType;

                DataGridViewCheckBoxCell chkApprovedCell = new DataGridViewCheckBoxCell();
                chkApprovedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                gvSettlementsInMedBill["Approved", 0] = chkApprovedCell;

                // Populate payment method type - 10-04-18 begin debugging here
                DataGridViewComboBoxCell comboCellPaymentType = new DataGridViewComboBoxCell();
                for (int i = 0; i < lstPaymentMethod.Count; i++)
                {
                    comboCellPaymentType.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                }
                gvSettlementsInMedBill["PaymentMethod", 0] = comboCellPaymentType;

                for (int i = 0; i < comboCellPaymentType.Items.Count - 1; i++)
                {
                    if (comboCellPaymentType.Items[i].ToString() == "None")
                        gvSettlementsInMedBill["PaymentMethod", 0].Value = comboCellPaymentType.Items[i];
                }

                // Populate credit cards
                DataGridViewComboBoxCell comboCellCreditCards = new DataGridViewComboBoxCell();
                for (int i = 0; i < lstCreditCardInfo.Count; i++)
                {
                    comboCellCreditCards.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                }

                gvSettlementsInMedBill["CreditCard", 0] = comboCellCreditCards;

                for (int i = 0; i < comboCellCreditCards.Items.Count; i++)
                {
                    if (comboCellCreditCards.Items[i].ToString() == "None")
                        gvSettlementsInMedBill["CreditCard", 0].Value = comboCellCreditCards.Items[i];
                }

                DataGridViewCheckBoxCell Reconciled = new DataGridViewCheckBoxCell();

                Reconciled.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                gvSettlementsInMedBill["Reconciled", 0] = Reconciled;

                //DataGridViewComboBoxCell comboIneligibleReasonCell = new DataGridViewComboBoxCell();
                gvSettlementsInMedBill["IneligibleReason", 0].Value = null;
                gvSettlementsInMedBill["IneligibleReason", 0].ReadOnly = true;

                btnSaveSettlement.Enabled = true;
                btnDeleteSettlement.Enabled = true;

            }
            else if (gvSettlementsInMedBill.Rows.Count > 0)
            {
                Boolean bNotEmpty = false;
                for (int i = 2; i < gvSettlementsInMedBill.ColumnCount; i++)
                {
                    if (gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells[i].Value != null)
                    {
                        bNotEmpty = true;
                        break;
                    }
                }
                if (bNotEmpty)
                {
                    gvSettlementsInMedBill.Rows.Add();
                    gvSettlementsInMedBill["Selected", gvSettlementsInMedBill.Rows.Count - 1].Value = true;
                    String NextSettlementName = gvSettlementsInMedBill["SettlementName", gvSettlementsInMedBill.Rows.Count - 2].Value.ToString();

                    int nNextSettlementNo = Int32.Parse(NextSettlementName.Substring(5));
                    nNextSettlementNo++;
                    NextSettlementName = NextSettlementName.Substring(0, 4) + "-" + nNextSettlementNo.ToString();

                    if (Int32.Parse(SettlementName.Substring(5)) >= Int32.Parse(NextSettlementName.Substring(5)))
                        gvSettlementsInMedBill["SettlementName", gvSettlementsInMedBill.Rows.Count - 1].Value = SettlementName;
                    else gvSettlementsInMedBill["SettlementName", gvSettlementsInMedBill.Rows.Count - 1].Value = NextSettlementName;

                    // Populate settlement type
                    DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();

                    for (int i = 0; i < lstSettlementType.Count; i++)
                    {
                        comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                    }
                    gvSettlementsInMedBill["SettlementTypeValue", gvSettlementsInMedBill.Rows.Count - 1] = comboCellSettlementType;



                    // Populate payment method type
                    DataGridViewComboBoxCell comboCellPaymentType = new DataGridViewComboBoxCell();
                    for (int i = 0; i < lstPaymentMethod.Count; i++)
                    {
                        if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentType.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                        else comboCellPaymentType.Items.Add(String.Empty);
                    }

                    gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1] = comboCellPaymentType;

                    for (int i = 0; i < comboCellPaymentType.Items.Count - 1; i++)
                    {
                        if (comboCellPaymentType.Items[i].ToString() == "None")
                            gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1].Value = comboCellPaymentType.Items[i];
                    }

                    // Approved check box
                    DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                    approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    gvSettlementsInMedBill["Approved", gvSettlementsInMedBill.Rows.Count - 1] = approvedCell;

                    // Populate credit cards
                    DataGridViewComboBoxCell comboCellCreditCards = new DataGridViewComboBoxCell();
                    for (int i = 0; i < lstCreditCardInfo.Count; i++)
                    {
                        if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCards.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                        else comboCellCreditCards.Items.Add(String.Empty);
                    }
                    gvSettlementsInMedBill["CreditCard", gvSettlementsInMedBill.Rows.Count - 1] = comboCellCreditCards;
                    for(int i = 0; i < comboCellCreditCards.Items.Count; i++)
                    {
                        if (comboCellCreditCards.Items[i].ToString() == String.Empty)
                            gvSettlementsInMedBill["CreditCard", gvSettlementsInMedBill.Rows.Count - 1].Value = comboCellCreditCards.Items[i];
                    }

                    if (gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1]?.Value?.ToString() == "Check")
                    {
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CheckNo"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["ACHNo"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CreditCard"].ReadOnly = true;
                    }

                    if (gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1]?.Value?.ToString() == "ACH/Banking")
                    {
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["ACHNo"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CheckNo"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CreditCard"].ReadOnly = true;
                    }

                    if (gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1]?.Value?.ToString() == "Credit Card")
                    {
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CreditCard"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CheckNo"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["ACHNo"].ReadOnly = true;
                    }
                    if (gvSettlementsInMedBill["PaymentMethod", gvSettlementsInMedBill.Rows.Count - 1]?.Value?.ToString() == String.Empty)
                    {
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CheckNo"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["ACHNo"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["CreditCard"].ReadOnly = true;

                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["PaymentDate"].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[gvSettlementsInMedBill.Rows.Count - 1].Cells["Reconciled"].ReadOnly = true;
                    }

                    DataGridViewCheckBoxCell ReconciledCel = new DataGridViewCheckBoxCell();
                    ReconciledCel.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                    gvSettlementsInMedBill["Reconciled", gvSettlementsInMedBill.Rows.Count - 1] = ReconciledCel;

                    gvSettlementsInMedBill["IneligibleReason", gvSettlementsInMedBill.Rows.Count - 1].Value = null;
                    gvSettlementsInMedBill["IneligibleReason", gvSettlementsInMedBill.Rows.Count - 1].ReadOnly = true;

                    btnSaveSettlement.Enabled = true;
                    btnDeleteSettlement.Enabled = true;

                }
                else
                {
                    MessageBox.Show("The last row is empty!");
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if ((Boolean)gvSettlementsInMedBill[0, i].Value == true)
                {
                    if (gvSettlementsInMedBill["SettlementTypeValue", i]?.Value == null)
                    {
                        MessageBox.Show("You have to select a Settlement Type.", "Alert");
                        return;
                    }

                    Decimal result, SettlementAmount;
                    if (Decimal.TryParse(gvSettlementsInMedBill["SettlementAmount", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result)) SettlementAmount = result;
                    else
                    {
                        MessageBox.Show("You have to enter decimal value in Settlement Amount field in Settlement " + gvSettlementsInMedBill[1, i]?.Value?.ToString(), "Alert");
                        return;
                    }

                    result = 0;
                    Decimal PersonalResponsibilityAmount = 0;

                    if ((gvSettlementsInMedBill["SettlementTypeValue", i]?.Value?.ToString() == "Self Pay Discount") ||
                        (gvSettlementsInMedBill["SettlementTypeValue", i]?.Value?.ToString() == "3rd Party Discount") ||
                        (gvSettlementsInMedBill["SettlementTypeValue", i]?.Value?.ToString() == "Member Payment"))
                    {
                        if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result)) PersonalResponsibilityAmount = result;
                        else
                        {
                            MessageBox.Show("You have to enter decimal value in Personal Responsibility field in Settlement " + gvSettlementsInMedBill[1, i]?.Value?.ToString(), "Alert");
                            return;
                        }
                    }

                    result = 0;
                    Decimal AllowedAmount = 0;
                    if (gvSettlementsInMedBill["AllowedAmount", i]?.Value?.ToString() != null)
                    {
                        if (Decimal.TryParse(gvSettlementsInMedBill["AllowedAmount", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result)) AllowedAmount = result;
                        else
                        {
                            MessageBox.Show("You have to enter decimal value in Allowed Amount field in Settlement " + gvSettlementsInMedBill[1, i]?.Value?.ToString(), "Alert");
                            return;
                        }
                    }
                }

                Decimal MedBillAmount = Decimal.Parse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"));
                Decimal SettlementAmountTotal = 0;

                for (int j = 0; j < gvSettlementsInMedBill.Rows.Count; j++)
                {
                    Decimal result = 0;
                    Decimal SettlementAmount = 0;
                    if (Decimal.TryParse(gvSettlementsInMedBill["SettlementAmount", j]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                    {
                        SettlementAmount = result;
                        SettlementAmountTotal += SettlementAmount;
                        if (SettlementAmountTotal > MedBillAmount)
                        {
                            MessageBox.Show("The total of settlement amount exceeds medical bill amount.");
                            return;
                        }
                    }
                }
            }

            Boolean bError = false;
            int nSelected = 0;

            for(int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if ((Boolean)gvSettlementsInMedBill["Selected", i].Value == true)
                {
                    nSelected++;
                }
            }

            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if ((Boolean)gvSettlementsInMedBill["Selected", i].Value == true)
                {
                    String strSqlQueryForSettlementType = "select [dbo].[tbl_settlement_type_code].[SettlementTypeCode], [dbo].[tbl_settlement_type_code].[SettlementTypeValue] " +
                                                          "from [dbo].[tbl_settlement_type_code]";

                    SqlCommand cmdQueryForSettlementType = new SqlCommand(strSqlQueryForSettlementType, connRN);
                    cmdQueryForSettlementType.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrSettlementType = cmdQueryForSettlementType.ExecuteReader();
                    lstSettlementType.Clear();
                    if (rdrSettlementType.HasRows)
                    {
                        while (rdrSettlementType.Read())
                        {
                            if (rdrSettlementType.GetInt16(0) > 0)
                                lstSettlementType.Add(new SettlementTypeInfo { SettlementTypeCode = rdrSettlementType.GetInt16(0), SettlementTypeValue = rdrSettlementType.GetString(1) });
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String SettlementName = gvSettlementsInMedBill["SettlementName", i].Value.ToString();
                    
                    // Check whether or not the settlement is already in data base
                    String strSqlQueryForSettlementName = "select [dbo].[tbl_settlement].[Name] from [dbo].[tbl_settlement] where [dbo].[tbl_settlement].[Name] = @Settlement";

                    SqlCommand cmdQueryForSettlementName = new SqlCommand(strSqlQueryForSettlementName, connRN);
                    cmdQueryForSettlementName.CommandType = CommandType.Text;

                    cmdQueryForSettlementName.Parameters.AddWithValue("@Settlement", SettlementName);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    Object objResultSettlementName = cmdQueryForSettlementName.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (objResultSettlementName == null)   // new settlement: save the settlement by using insert sql statement
                    {

                        String strSqlCreateNewSettlement = "insert into [dbo].[tbl_settlement] (IsDeleted, Name, CreateDate, CreateByID, LastModifiedDate, LastModifiedByID, SystemModifiedStamp, " +
                                                           "LastActivityDate, LastViewedDate, MedicalBillID, " +
                                                           "SettlementType, Amount, PersonalResponsibilityCredit, CMMPaymentMethod, " +
                                                           "CheckNo, CheckDate, CheckReconciled, ACH_Number, ACH_Date, ACH_Reconciled, CMMCreditCard, CMMCreditCardPaidDate, CC_Reconciled, " +
                                                           "AllowedAmount, Notes, Approved, ApprovedDate, IneligibleReason) " +
                                                           "values (0, @SettlementName, @CreateDate, @CreateByID, @LastModifiedDate, @LastModifiedByID, @SystemModifiedStamp, " +
                                                           "@LastActivityDate, @LastViewedDate, @MedBillID, " +
                                                           "@SettlementType, @SettlementAmount, @PersonalResponsibilityCredit, @CMMPaymentMethod, " +
                                                           "@CheckNo, @CheckDate, @IsCheckReconciled, @ACH_Number, @ACH_Date, @IsACH_Reconciled, @CMMCreditCard, @CMMCreditCardPaidDate, @IsCC_Reconciled, " +
                                                           "@AllowedAmount, @Notes, @IsApproved, @ApprovedDate, @IneligibleReason)";

                        SqlCommand cmdInsertNewSettlement = new SqlCommand(strSqlCreateNewSettlement, connRN);
                        cmdInsertNewSettlement.CommandType = CommandType.Text;

                        String NewSettlementName = gvSettlementsInMedBill["SettlementName", i].Value.ToString();
                        String NewSettlementType = String.Empty;
                        if (gvSettlementsInMedBill["SettlementTypeValue", i].Value != null) NewSettlementType = gvSettlementsInMedBill["SettlementTypeValue", i].Value.ToString();
                        int nNewSettlementType = 0;
                        for (int j = 0; j < lstSettlementType.Count; j++)
                        {
                            if (NewSettlementType == lstSettlementType[j].SettlementTypeValue) nNewSettlementType = lstSettlementType[j].SettlementTypeCode;
                        }
                        Decimal result = 0;
                        Decimal SettlementAmount = 0;
                        if (Decimal.TryParse(gvSettlementsInMedBill["SettlementAmount", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                        {
                            SettlementAmount = result;
                            cmdInsertNewSettlement.Parameters.AddWithValue("@SettlementAmount", SettlementAmount);
                        }

                        result = 0;
                        Decimal PersonalResponsibilityAmount = 0;
                        if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result)) PersonalResponsibilityAmount = result;
                        cmdInsertNewSettlement.Parameters.AddWithValue("@PersonalResponsibilityCredit", PersonalResponsibilityAmount);

                        int NewIsApproved = 0;
                        if (gvSettlementsInMedBill["Approved", i]?.Value != null)
                        {
                            if ((Boolean)gvSettlementsInMedBill["Approved", i]?.Value == true) NewIsApproved = 1;
                        }
                        DateTime? NewApprovedDate = null;
                        if (gvSettlementsInMedBill["ApprovedDate", i]?.Value != null) NewApprovedDate = DateTime.Parse(gvSettlementsInMedBill["ApprovedDate", i].Value.ToString());

                        String NewCMMPaymentMethod = String.Empty;
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value != null) NewCMMPaymentMethod = gvSettlementsInMedBill["PaymentMethod", i].Value.ToString();
                        int nNewCMMPaymentMethod = 0;
                        for (int j = 0; j < lstPaymentMethod.Count; j++)
                        {
                            if (NewCMMPaymentMethod == lstPaymentMethod[j].PaymentMethodValue) nNewCMMPaymentMethod = lstPaymentMethod[j].PaymentMethodId;
                        }

                        String NewCheckNo = String.Empty;
                        DateTime? NewCheckDate = null;
                        int NewCheckReconciled = 0;
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Check")
                        {
                            if (gvSettlementsInMedBill["CheckNo", i].Value != null) NewCheckNo = gvSettlementsInMedBill["CheckNo", i].Value.ToString();
                            if (gvSettlementsInMedBill["PaymentDate", i].Value != null) NewCheckDate = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                            if (gvSettlementsInMedBill["Reconciled", i].Value != null) NewCheckReconciled = 1;
                        }

                        String NewACH_Number = String.Empty;
                        DateTime? NewACH_Date = null;
                        int NewACH_Reconciled = 0;
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "ACH/Banking")
                        {
                            if (gvSettlementsInMedBill["ACHNo", i].Value != null) NewACH_Number = gvSettlementsInMedBill["ACHNo", i].Value.ToString();
                            if (gvSettlementsInMedBill["PaymentDate", i].Value != null) NewACH_Date = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                            if (gvSettlementsInMedBill["Reconciled", i].Value != null) NewACH_Reconciled = 1;
                        }

                        int nCMMCreditCard = 0;
                        DateTime? NewCreditCardPaidDate = null;
                        int NewIsCCReconciled = 0;

                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Credit Card")
                        {
                            String CreditCard = gvSettlementsInMedBill[13, i]?.Value?.ToString();
                            for (int j = 0; j < lstCreditCardInfo.Count; j++)
                            {
                                if (CreditCard == lstCreditCardInfo[j].CreditCardNo)
                                {
                                    nCMMCreditCard = lstCreditCardInfo[j].CreditCardId;
                                }
                            }
                            if (gvSettlementsInMedBill["PaymentDate", i].Value != null)
                                NewCreditCardPaidDate = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                            if (gvSettlementsInMedBill["Reconciled", i].Value != null) NewIsCCReconciled = 1;
                        }

                        Decimal NewAllowedAmount = 0;
                        if (gvSettlementsInMedBill["AllowedAmount", i].Value != null) NewAllowedAmount = Decimal.Parse(gvSettlementsInMedBill["AllowedAmount", i].Value.ToString());
                        String NewNote = String.Empty;
                        if (gvSettlementsInMedBill["Note", i].Value != null) NewNote = gvSettlementsInMedBill["Note", i].Value.ToString();


                        cmdInsertNewSettlement.Parameters.AddWithValue("@SettlementName", NewSettlementName);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@CreateDate", DateTime.Today);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@CreateByID", nLoggedUserId);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@LastModifiedByID", nLoggedUserId);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@SystemModifiedStamp", DateTime.Today);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@LastViewedDate", DateTime.Today);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@MedBillID", txtMedBillNo.Text.Trim());
                        cmdInsertNewSettlement.Parameters.AddWithValue("@SettlementType", nNewSettlementType);

                        cmdInsertNewSettlement.Parameters.AddWithValue("@IsApproved", NewIsApproved);

                        if (NewApprovedDate != null) cmdInsertNewSettlement.Parameters.AddWithValue("@ApprovedDate", NewApprovedDate);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@ApprovedDate", DBNull.Value);

                        cmdInsertNewSettlement.Parameters.AddWithValue("@CMMPaymentMethod", nNewCMMPaymentMethod);

                        if (NewCheckNo != String.Empty) cmdInsertNewSettlement.Parameters.AddWithValue("@CheckNo", NewCheckNo);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@CheckNo", DBNull.Value);

                        if (NewCheckDate != null) cmdInsertNewSettlement.Parameters.AddWithValue("@CheckDate", NewCheckDate);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@CheckDate", DBNull.Value);

                        cmdInsertNewSettlement.Parameters.AddWithValue("@IsCheckReconciled", NewCheckReconciled);

                        if (NewACH_Number != null) cmdInsertNewSettlement.Parameters.AddWithValue("@ACH_Number", NewACH_Number);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@ACH_Number", DBNull.Value);

                        if (NewACH_Date != null) cmdInsertNewSettlement.Parameters.AddWithValue("@ACH_Date", NewACH_Date);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@ACH_Date", DBNull.Value);

                        cmdInsertNewSettlement.Parameters.AddWithValue("@IsACH_Reconciled", NewACH_Reconciled);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@CMMCreditCard", nCMMCreditCard);

                        if (NewCreditCardPaidDate != null) cmdInsertNewSettlement.Parameters.AddWithValue("@CMMCreditCardPaidDate", NewCreditCardPaidDate);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@CMMCreditCardPaidDate", DBNull.Value);

                        cmdInsertNewSettlement.Parameters.AddWithValue("@IsCC_Reconciled", NewIsCCReconciled);
                        cmdInsertNewSettlement.Parameters.AddWithValue("@AllowedAmount", NewAllowedAmount);

                        if (NewNote != null) cmdInsertNewSettlement.Parameters.AddWithValue("@Notes", NewNote);
                        else cmdInsertNewSettlement.Parameters.AddWithValue("@Notes", DBNull.Value);

                        int nIneligibleReason = 0;
                        for (int j = 0; j < dicIneligibleReason.Count; j++)
                        {
                            if (gvSettlementsInMedBill["IneligibleReason", i]?.Value?.ToString() == dicIneligibleReason[j]) nIneligibleReason = j;
                        }
                        cmdInsertNewSettlement.Parameters.AddWithValue("@IneligibleReason", nIneligibleReason);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nRowInserted = cmdInsertNewSettlement.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        //if (nRowInserted == 1)
                        //{
                        //    MessageBox.Show("Settlements have been saved.", "Information");
                        //    return;
                        //}
                        //else
                        //{
                        //    MessageBox.Show("Some of settlement have not been saved.", "Error");
                        //}

                        if (nRowInserted == 0) bError = true;

                    }
                    else  // the settlement with the name exist, update the settlement
                    {
                        String UpdateSettlementName = objResultSettlementName.ToString();
                        String UpdateMedBill = txtMedBillNo.Text.Trim();

                        String strSqlUpdateSettlement = "update [dbo].[tbl_settlement] set [dbo].[tbl_settlement].[LastModifiedDate] = @LastModifiedDate, " +
                                                        "[dbo].[tbl_settlement].[LastModifiedByID] = @LastModifiedByID, " +
                                                        "[dbo].[tbl_settlement].[LastActivityDate] = @LastActivityDate, " +
                                                        "[dbo].[tbl_settlement].[SettlementType] = @SettlementType, " +
                                                        "[dbo].[tbl_settlement].[Amount] = @SettlementAmount, " +
                                                        "[dbo].[tbl_settlement].[PersonalResponsibilityCredit] = @PersonalResponsibilityCredit, " +
                                                        "[dbo].[tbl_settlement].[Approved] = @IsApproved, " +
                                                        "[dbo].[tbl_settlement].[ApprovedDate] = @ApprovedDate, " +
                                                        "[dbo].[tbl_settlement].[CMMPaymentMethod] = @CMMPaymentMethod, " +
                                                        "[dbo].[tbl_settlement].[CheckNo] = @CheckNo, " +
                                                        "[dbo].[tbl_settlement].[CheckDate] = @CheckDate, " +
                                                        "[dbo].[tbl_settlement].[CheckReconciled] = @CheckReconciled, " +
                                                        "[dbo].[tbl_settlement].[ACH_Number] = @ACH_Number, " +
                                                        "[dbo].[tbl_settlement].[ACH_Date] = @ACH_Date, " +
                                                        "[dbo].[tbl_settlement].[ACH_Reconciled] = @ACH_Reconciled, " +
                                                        "[dbo].[tbl_settlement].[CMMCreditCard] = @CMMCreditCard, " +
                                                        "[dbo].[tbl_settlement].[CMMCreditCardPaidDate] = @CMMCreditCardPaidDate, " +
                                                        "[dbo].[tbl_settlement].[CC_Reconciled] = @CC_Reconciled, " +
                                                        "[dbo].[tbl_settlement].[AllowedAmount] = @AllowedAmount, " +
                                                        "[dbo].[tbl_settlement].[Notes] = @Note, " +
                                                        "[dbo].[tbl_settlement].[IneligibleReason] = @IneligibleReason " +
                                                        "where [dbo].[tbl_settlement].[Name] = @SettlementName and [dbo].[tbl_settlement].[MedicalBillID] = @MedBillName";

                        int nSettlementType = 0;
                        String strSettlementType = String.Empty;
                        if (gvSettlementsInMedBill["SettlementTypeValue", i].Value != null) strSettlementType = gvSettlementsInMedBill["SettlementTypeValue", i].Value.ToString();
                        for (int j = 0; j < lstSettlementType.Count; j++)
                        {
                            if (strSettlementType == lstSettlementType[j].SettlementTypeValue) nSettlementType = lstSettlementType[j].SettlementTypeCode;
                        }

                        Decimal SettlementAmount = 0;
                        if (gvSettlementsInMedBill["SettlementAmount", i].Value != null)
                        {
                            SettlementAmount = Decimal.Parse(gvSettlementsInMedBill["SettlementAmount", i].Value.ToString(), NumberStyles.Currency);
                        }

                        Decimal PersonalResponsibilityAmt = 0;
                        if (gvSettlementsInMedBill["PersonalResponsibility", i].Value != null)
                        {
                            PersonalResponsibilityAmt = Decimal.Parse(gvSettlementsInMedBill["PersonalResponsibility", i].Value.ToString(), NumberStyles.Currency);
                        }

                        // Payment method
                        int nPaymentMethod = 0;
                        String PaymentMethod = String.Empty;
                        if (gvSettlementsInMedBill["PaymentMethod", i].Value != null)
                            PaymentMethod = gvSettlementsInMedBill["PaymentMethod", i].Value.ToString();
                        for (int j = 0; j < lstPaymentMethod.Count; j++)
                        {
                            if (PaymentMethod == lstPaymentMethod[j].PaymentMethodValue) nPaymentMethod = lstPaymentMethod[j].PaymentMethodId;
                        }

                        // Approved or not
                        int nApproved = 0;
                        if (gvSettlementsInMedBill["Approved", i].Value != null)
                        {
                            if ((Boolean)gvSettlementsInMedBill["Approved", i].Value) nApproved = 1;
                        }

                        DateTime? ApprovedDate = null;
                        if (gvSettlementsInMedBill["ApprovedDate", i].Value != null) ApprovedDate = DateTime.Parse(gvSettlementsInMedBill["ApprovedDate", i].Value.ToString());


                        String CheckNo = String.Empty;
                        DateTime? CheckIssueDate = null;
                        int nCheckReconciled = 0;

                        String ACH_No = String.Empty;
                        DateTime? ACH_Date = null;
                        int nACHReconciled = 0;

                        String CreditCard = String.Empty;
                        int nCreditCard = 0;
                        DateTime? CreditCardPaidDate = null;
                        int nCCReconciled = 0;

                        switch (PaymentMethod)
                        {
                            case "Check":
                                if (gvSettlementsInMedBill["CheckNo", i].Value != null) CheckNo = gvSettlementsInMedBill["CheckNo", i].Value.ToString();
                                if (gvSettlementsInMedBill["PaymentDate", i].Value != null)
                                {
                                    if (gvSettlementsInMedBill["PaymentDate", i].Value.ToString() != String.Empty) CheckIssueDate = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                                }
                                Boolean bCheckReconciledResult = false;
                                if (gvSettlementsInMedBill["Reconciled", i].Value != null)
                                {
                                    if (Boolean.TryParse(gvSettlementsInMedBill["Reconciled", i].Value.ToString(), out bCheckReconciledResult))
                                    {
                                        if (bCheckReconciledResult) nCheckReconciled = 1;
                                    }
                                }
                                break;
                            case "ACH/Banking":
                                if (gvSettlementsInMedBill["ACHNo", i].Value != null) ACH_No = gvSettlementsInMedBill["ACHNo", i].Value.ToString();
                                if (gvSettlementsInMedBill["PaymentDate", i].Value != null)
                                {
                                    if (gvSettlementsInMedBill["PaymentDate", i].Value.ToString() != String.Empty) ACH_Date = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                                }
                                Boolean bACHReconciledResult = false;
                                if (gvSettlementsInMedBill["Reconciled", i].Value != null)
                                {
                                    if (Boolean.TryParse(gvSettlementsInMedBill["Reconciled", i].Value.ToString(), out bACHReconciledResult))
                                    {
                                        if (bACHReconciledResult) nACHReconciled = 1;
                                    }
                                }
                                break;
                            case "Credit Card":
                                if (gvSettlementsInMedBill["CreditCard", i].Value != null) CreditCard = gvSettlementsInMedBill["CreditCard", i].Value.ToString();
                                for (int j = 0; j < lstCreditCardInfo.Count; j++)
                                {
                                    if (CreditCard == lstCreditCardInfo[j].CreditCardNo) nCreditCard = lstCreditCardInfo[j].CreditCardId;
                                }
                                if (gvSettlementsInMedBill["PaymentDate", i].Value != null)
                                {
                                    if (gvSettlementsInMedBill["PaymentDate", i].Value.ToString() != String.Empty)
                                        CreditCardPaidDate = DateTime.Parse(gvSettlementsInMedBill["PaymentDate", i].Value.ToString());
                                }
                                Boolean bCCReconciledResult = false;
                                if (gvSettlementsInMedBill[15, i].Value != null)
                                {
                                    if (Boolean.TryParse(gvSettlementsInMedBill["Reconciled", i].Value.ToString(), out bCCReconciledResult))
                                    {
                                        if (bCCReconciledResult) nCCReconciled = 1;
                                    }
                                }
                                break;
                        }

                        // Allowed Amount
                        Decimal AllowedAmount = 0;
                        if (gvSettlementsInMedBill["AllowedAmount", i].Value != null)
                        {
                            AllowedAmount = Decimal.Parse(gvSettlementsInMedBill["AllowedAmount", i].Value.ToString(), NumberStyles.Currency);
                        }

                        // Note
                        String Note = String.Empty;
                        if (gvSettlementsInMedBill["Note", i].Value != null) Note = gvSettlementsInMedBill["Note", i].Value.ToString();



                        SqlCommand cmdUpdateSettlement = new SqlCommand(strSqlUpdateSettlement, connRN);
                        cmdUpdateSettlement.CommandType = CommandType.Text;

                        cmdUpdateSettlement.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                        cmdUpdateSettlement.Parameters.AddWithValue("@LastModifiedByID", nLoggedUserId);
                        cmdUpdateSettlement.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                        cmdUpdateSettlement.Parameters.AddWithValue("@SettlementType", nSettlementType);


                        cmdUpdateSettlement.Parameters.AddWithValue("@SettlementAmount", SettlementAmount);
                        cmdUpdateSettlement.Parameters.AddWithValue("@PersonalResponsibilityCredit", PersonalResponsibilityAmt);

                        cmdUpdateSettlement.Parameters.AddWithValue("@IsApproved", nApproved);
                        if (ApprovedDate != null) cmdUpdateSettlement.Parameters.AddWithValue("@ApprovedDate", ApprovedDate);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@ApprovedDate", DBNull.Value);

                        cmdUpdateSettlement.Parameters.AddWithValue("@CMMPaymentMethod", nPaymentMethod);

                        if (CheckNo != String.Empty) cmdUpdateSettlement.Parameters.AddWithValue("@CheckNo", CheckNo);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@CheckNo", DBNull.Value);

                        if (CheckIssueDate != null) cmdUpdateSettlement.Parameters.AddWithValue("@CheckDate", CheckIssueDate);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@CheckDate", DBNull.Value);

                        cmdUpdateSettlement.Parameters.AddWithValue("@CheckReconciled", nCheckReconciled);

                        if (ACH_No != String.Empty) cmdUpdateSettlement.Parameters.AddWithValue("@ACH_Number", ACH_No);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@ACH_Number", DBNull.Value);

                        if (ACH_Date != null) cmdUpdateSettlement.Parameters.AddWithValue("@ACH_Date", ACH_Date);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@ACH_Date", DBNull.Value);

                        cmdUpdateSettlement.Parameters.AddWithValue("@ACH_Reconciled", nACHReconciled);
                        cmdUpdateSettlement.Parameters.AddWithValue("@CMMCreditCard", nCreditCard);

                        if (CreditCardPaidDate != null) cmdUpdateSettlement.Parameters.AddWithValue("@CMMCreditCardPaidDate", CreditCardPaidDate);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@CMMCreditCardPaidDate", DBNull.Value);
                        cmdUpdateSettlement.Parameters.AddWithValue("@CC_Reconciled", nCCReconciled);
                        cmdUpdateSettlement.Parameters.AddWithValue("@AllowedAmount", AllowedAmount);
                        if (Note != String.Empty) cmdUpdateSettlement.Parameters.AddWithValue("@Note", Note);
                        else cmdUpdateSettlement.Parameters.AddWithValue("@Note", DBNull.Value);

                        cmdUpdateSettlement.Parameters.AddWithValue("@SettlementName", UpdateSettlementName);
                        cmdUpdateSettlement.Parameters.AddWithValue("@MedBillName", UpdateMedBill);

                        int nIneligibleReason = 0;
                        for (int j = 0; j < dicIneligibleReason.Count; j++)
                        {
                            if (gvSettlementsInMedBill["IneligibleReason", i]?.Value?.ToString() == dicIneligibleReason[j]) nIneligibleReason = j;
                        }
                        cmdUpdateSettlement.Parameters.AddWithValue("@IneligibleReason", nIneligibleReason);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        int nRowUpdated = cmdUpdateSettlement.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        //if (nRowUpdated == 1)
                        //{
                        //    MessageBox.Show("Settlements have been saved.", "Information");
                        //    return;
                        //}
                        //else
                        //{
                        //    MessageBox.Show("Some of settlement have not been saved.", "Error");
                        //}

                        if (nRowUpdated == 0) bError = true;
                    }
                }
            }

            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if (gvSettlementsInMedBill["SettlementTypeValue", i]?.Value?.ToString() != "Ineligible")
                {
                    gvSettlementsInMedBill["IneligibleReason", i].Value = null;
                    gvSettlementsInMedBill["IneligibleReason", i].ReadOnly = true;
                }
            }

            if ((bError == false)&&(nSelected > 0))
            {
                MessageBox.Show("Settlements have been saved.", "Info");
                return;
            }
            else if ((bError == true)&&(nSelected > 0))
            {
                MessageBox.Show("Some of settlments have not been saved.", "Error");
                return;
            }
        }

        private void OnSettlementsInMedBillChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnSettlementsInMedBillChange;

                UpdateGridViewSettlementsInMedBill();
            }

        }

        private void UpdateGridViewSettlementsInMedBill()
        {
            //String MedicalBillNo = String.Empty;
            //MedicalBillNo = gvMedBill[1, nRowSelected].Value.ToString();




            String strSqlQueryForSettlementsInMedBill = "select [dbo].[tbl_settlement].[SelfPayDiscount], [dbo].[tbl_settlement].[_3rdPartyDiscount], [dbo].[tbl_settlement].[MemberPayment], " +
                                            "[dbo].[tbl_settlement].[CMMProviderPayment], [dbo].[tbl_settlement].[Eligibility], [dbo].[tbl_settlement].[AllowedAmount], " +
                                            "[dbo].[tbl_settlement].[CMMDiscount], [dbo].[tbl_settlement].[MemberReimbursement], " +
                                            "[dbo].[tbl_payment_method].[PaymentMethod_Value], [dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
                                            "[dbo].[tbl_Credit_Card__c].[Name], [dbo].[tbl_settlement].[CMMCreditCardPaidDay], [dbo].[tbl_settlement].[CC_Reconciled], " +
                                            "[dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], [dbo].[tbl_settlement].[ACH_Reconciled],  " +
                                            "[dbo].[tbl_settlement].[Notes], [dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate] " +
                                            "from (([dbo].[tbl_settlement] inner join [dbo].[tbl_Credit_Card__c] on [dbo].[tbl_settlement].[CMMCreditCard] = [dbo].[tbl_Credit_Card__c].[CreditCard_Id]) " +
                                            "inner join [dbo].[tbl_payment_method] on [dbo].[tbl_settlement].[CMMPaymentMethod] = [dbo].[tbl_payment_method].[PaymentMethod_Id]) " +
                                            "where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillNo";


            SqlCommand cmdQueryForSettlementInMedBill = new SqlCommand(strSqlQueryForSettlementsInMedBill, connRN);
            cmdQueryForSettlementInMedBill.CommandType = CommandType.Text;

            cmdQueryForSettlementInMedBill.Parameters.AddWithValue("@MedBillNo", MedicalBillNo);
            cmdQueryForSettlementInMedBill.Notification = null;

            SqlDependency dependencySettlements = new SqlDependency(cmdQueryForSettlementInMedBill);
            dependencySettlements.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrSettlements = cmdQueryForSettlementInMedBill.ExecuteReader();

            gvSettlementsInMedBill.Rows.Clear();
            if (rdrSettlements.HasRows)
            {

                while (rdrSettlements.Read())
                {

                    DataGridViewRow row = new DataGridViewRow();
                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    if (!rdrSettlements.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(0).ToString("C") });
                    if (!rdrSettlements.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(1).ToString("C") });
                    if (!rdrSettlements.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(2).ToString("C") });
                    if (!rdrSettlements.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(3).ToString("C") });
                    if (!rdrSettlements.IsDBNull(4))
                    {
                        DataGridViewComboBoxCell cellEligibility = new DataGridViewComboBoxCell();
                        cellEligibility.Items.Add("Yes");
                        cellEligibility.Items.Add("No");

                        if (rdrSettlements.GetBoolean(4)) cellEligibility.Value = "Yes";
                        else cellEligibility.Value = "No";

                        row.Cells.Add(cellEligibility);
                    }
                    if (!rdrSettlements.IsDBNull(5)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(5).ToString("C") });
                    if (!rdrSettlements.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(6).ToString("C") });
                    if (!rdrSettlements.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(7).ToString("C") });
                    if (!rdrSettlements.IsDBNull(8))
                    {
                        DataGridViewComboBoxCell cellPaymentMethod = new DataGridViewComboBoxCell();

                        foreach (PaymentMethod paymt_method in lstPaymentMethod)
                        {
                            cellPaymentMethod.Items.Add(paymt_method.PaymentMethodValue);
                        }

                        cellPaymentMethod.Value = rdrSettlements.GetString(8);

                        row.Cells.Add(cellPaymentMethod);
                    }
                    if (!rdrSettlements.IsDBNull(9)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(9) });
                    if (!rdrSettlements.IsDBNull(10)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(10) });
                    if (!rdrSettlements.IsDBNull(11)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlements.GetBoolean(11) });

                    // Credit Card Info
                    if (!rdrSettlements.IsDBNull(12))
                    {
                        DataGridViewComboBoxCell cellCreditCard = new DataGridViewComboBoxCell();

                        foreach (CreditCardInfo info in lstCreditCardInfo)
                        {
                            cellCreditCard.Items.Add(info.CreditCardNo);
                        }

                        cellCreditCard.Value = rdrSettlements.GetString(12);
                        row.Cells.Add(cellCreditCard);
                    }
                    if (!rdrSettlements.IsDBNull(13)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(13) });
                    if (!rdrSettlements.IsDBNull(14)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlements.GetBoolean(14) });
                    if (!rdrSettlements.IsDBNull(15)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(15) });
                    if (!rdrSettlements.IsDBNull(16)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(16) });
                    if (!rdrSettlements.IsDBNull(17)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlements.GetBoolean(17) });
                    if (!rdrSettlements.IsDBNull(18)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(18) });
                    if (!rdrSettlements.IsDBNull(19)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlements.GetBoolean(19) });
                    if (!rdrSettlements.IsDBNull(20)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(20) });

                    gvSettlementsInMedBill.Rows.Add(row);

                }

                for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                {
                    if (gvSettlementsInMedBill[5, i].Value.ToString() == "No") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();


            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //String strSqlQueryForSettlementsInMedBill = "select [dbo].[tbl_settlement].[ServiceDate], [dbo].[tbl_settlement].[Description_CPT_Code], [dbo].[tbl_settlement].[Amount], " +
            //                                            "[dbo].[tbl_settlement].[SelfPayDiscount], [dbo].[tbl_settlement].[_3rdPartyDiscount], [dbo].[tbl_settlement].[MemberPayment], " +
            //                                            "[dbo].[tbl_settlement].[RemainingBalance], [dbo].[tbl_settlement].[CMMProviderPayment], [dbo].[tbl_settlement].[MedicareValue], " +
            //                                            "[dbo].[tbl_settlement].[Eligibility], [dbo].[tbl_settlement].[AllowedAmount], [dbo].[tbl_settlement].[CMMDiscount], " +
            //                                            "[dbo].[tbl_settlement].[MemberReimbursement], [dbo].[tbl_settlement].[CMMPaymentMethod], [dbo].[tbl_settlement].[AdjustmentFinalBalance], " +
            //                                            "[dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
            //                                            "[dbo].[tbl_settlement].[CMMCreditCard], [dbo].[tbl_settlement].[CMMCreditCardPopulated], [dbo].[tbl_settlement].[CMMCreditCardPaidDay], " +
            //                                            "[dbo].[tbl_settlement].[CC_Reconciled], [dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], " +
            //                                            "[dbo].[tbl_settlement].[ACH_Reconciled], [dbo].[tbl_settlement].[Notes], " +
            //                                            "[dbo].[tbl_settlement].[AmountOver150KIncidentLimitFormula], [dbo].[tbl_settlement].[AmountOver150KIncidentLimitSnapShot], " +
            //                                            "[dbo].[tbl_settlement].[WellBeingCare], [dbo].[tbl_settlement].[PersonalResponsibilityAmountForReset], " +
            //                                            "[dbo].[tbl_settlement].[WellBeingCareSharedForReset], [dbo].[tbl_settlement].[MedicalBillDate], " +
            //                                            "[dbo].[tbl_settlement].[PersonalResponsibilityForBill], " +
            //                                            "[dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate] " +
            //                                            "from [dbo].[tbl_settlement] where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillId";

            //SqlCommand cmdQueryForSettlementInMedBill = new SqlCommand(strSqlQueryForSettlementsInMedBill, connRN);
            //cmdQueryForSettlementInMedBill.CommandType = CommandType.Text;

            //cmdQueryForSettlementInMedBill.Parameters.AddWithValue("@MedBillId", MedicalBillNo);
            //cmdQueryForSettlementInMedBill.Notification = null;

            //SqlDependency dependencySettlements = new SqlDependency(cmdQueryForSettlementInMedBill);
            //dependencySettlements.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillChange);

            //connRN.Open();
            //SqlDataReader rdrSettlements = cmdQueryForSettlementInMedBill.ExecuteReader();

            //gvSettlementsInMedBill.Rows.Clear();
            //if (rdrSettlements.HasRows)
            //{
                
            //    while (rdrSettlements.Read())
            //    {
            //        DataGridViewRow row = new DataGridViewRow();
            //        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
            //        if (!rdrSettlements.IsDBNull(0)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(0).ToString("MM/dd/yyyy") });
            //        if (!rdrSettlements.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(1) });
            //        if (!rdrSettlements.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(2) });
            //        if (!rdrSettlements.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(3) });
            //        if (!rdrSettlements.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(4) });
            //        if (!rdrSettlements.IsDBNull(5)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(5) });
            //        if (!rdrSettlements.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(6) });
            //        if (!rdrSettlements.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(7) });
            //        if (!rdrSettlements.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(8) });
            //        DataGridViewComboBoxCell cellEligibility = new DataGridViewComboBoxCell();
            //        cellEligibility.Items.Add("Yes");
            //        cellEligibility.Items.Add("No");
            //        if (!rdrSettlements.IsDBNull(9))
            //        {
            //            if (rdrSettlements.GetBoolean(9)) cellEligibility.Value = "Yes";
            //            else cellEligibility.Value = "No";
            //        }
            //        row.Cells.Add(cellEligibility);


            //        if (!rdrSettlements.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(10) });
            //        if (!rdrSettlements.IsDBNull(11)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(11) });
            //        if (!rdrSettlements.IsDBNull(12)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(12) });
            //        DataGridViewComboBoxCell cellPaymentMethod = new DataGridViewComboBoxCell();
            //        cellPaymentMethod.Items.Add("Check");
            //        cellPaymentMethod.Items.Add("Credit Card");
            //        cellPaymentMethod.Items.Add("ACH");
            //        if (!rdrSettlements.IsDBNull(13))
            //        {
            //            switch (rdrSettlements.GetInt16(13))
            //            {
            //                case 0:
            //                    cellPaymentMethod.Value = "Check";
            //                    break;
            //                case 1:
            //                    cellPaymentMethod.Value = "Credit Card";
            //                    break;
            //                case 2:
            //                    cellPaymentMethod.Value = "ACH";
            //                    break;
            //            }
            //        }
            //        row.Cells.Add(cellPaymentMethod);

            //        if (!rdrSettlements.IsDBNull(14)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(14) });
            //        if (!rdrSettlements.IsDBNull(15)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(15) });
            //        if (!rdrSettlements.IsDBNull(16)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(16).ToString("MM/dd/yyyy") });
            //        if (!rdrSettlements.IsDBNull(17)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(17) });
            //        DataGridViewComboBoxCell cellCreditCard = new DataGridViewComboBoxCell();
            //        cellCreditCard.Items.Add("2383");
            //        cellCreditCard.Items.Add("5482");
            //        cellCreditCard.Items.Add("2959");
            //        if (!rdrSettlements.IsDBNull(18))
            //        {
            //            switch (rdrSettlements.GetInt16(18))
            //            {
            //                case 0:
            //                    cellCreditCard.Value = "2383";
            //                    break;
            //                case 1:
            //                    cellCreditCard.Value = "5482";
            //                    break;
            //                case 2:
            //                    cellCreditCard.Value = "2959";
            //                    break;
            //            }
            //            row.Cells.Add(cellCreditCard);
            //        }

            //        if (!rdrSettlements.IsDBNull(19)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(19) });
            //        if (!rdrSettlements.IsDBNull(20)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(20).ToString("MM/dd/yyyy") });
            //        if (!rdrSettlements.IsDBNull(21)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(21) });
            //        if (!rdrSettlements.IsDBNull(22)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(22) });
            //        if (!rdrSettlements.IsDBNull(23)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(23).ToString("MM/dd/yyyy") });
            //        if (!rdrSettlements.IsDBNull(24)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(24) });
            //        if (!rdrSettlements.IsDBNull(25)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(25) });
            //        if (!rdrSettlements.IsDBNull(26)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetString(26) });
            //        if (!rdrSettlements.IsDBNull(27)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(27) });
            //        if (!rdrSettlements.IsDBNull(28)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(28) });
            //        if (!rdrSettlements.IsDBNull(29)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(29) });
            //        if (!rdrSettlements.IsDBNull(30)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(30) });
            //        if (!rdrSettlements.IsDBNull(31)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(31).ToString("MM/dd/yyyy") });
            //        if (!rdrSettlements.IsDBNull(32)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlements.GetDecimal(32) });
            //        if (!rdrSettlements.IsDBNull(33)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlements.GetBoolean(33) });
            //        if (!rdrSettlements.IsDBNull(34)) row.Cells.Add(new CalendarCell { Value = rdrSettlements.GetDateTime(34).ToString("MM/dd/yyyy") });

            //        gvSettlementsInMedBill.Rows.Add(row);
            //    }

            //    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            //    {
            //        if (gvSettlementsInMedBill[10, i].Value.ToString() == "No") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
            //    }
            //}

            //connRN.Close();
        }

        private void gvSettlementsInMedBill_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gvSettlementsInMedBill.IsCurrentCellDirty)
            {
                gvSettlementsInMedBill.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void gvSettlementsInMedBill_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (gvSettlementsInMedBill.Rows.Count > 0)
            {
                if (e.ColumnIndex == 2)     // if the settlement is ineligible, change background color to red
                {
                    if (gvSettlementsInMedBill[2, e.RowIndex]?.Value?.ToString() == "Ineligible")
                    {
                        if (dicIneligibleReason.Count > 0)
                        {
                            DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                            for (int i = 0; i < dicIneligibleReason.Count; i++)
                            {
                                comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                            }
                            comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[0];
                            gvSettlementsInMedBill["IneligibleReason", e.RowIndex].ReadOnly = false;
                            gvSettlementsInMedBill["IneligibleReason", e.RowIndex] = comboCellIneligibleReason;
                        }
                        gvSettlementsInMedBill.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Red;
                    }
                    else
                    {
                        gvSettlementsInMedBill["IneligibleReason", e.RowIndex].Value = null;
                        gvSettlementsInMedBill["IneligibleReason", e.RowIndex].ReadOnly = true;
                        gvSettlementsInMedBill.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                    }
                }
                if (e.ColumnIndex == 3)     // show alert if the settlement amount total exceed med bill amount
                {

                    Decimal MedBillAmount = Decimal.Parse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"));
                    Decimal Balance = 0;
                    Decimal SettlementTotal = 0;
                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        Decimal settlementAmount = 0;
                        Decimal result = 0;
                        if (gvSettlementsInMedBill["SettlementAmount", i].Value != null)
                        {
                            if (Decimal.TryParse(gvSettlementsInMedBill["SettlementAmount", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                            {
                                settlementAmount = result;
                                SettlementTotal += settlementAmount;
                                Balance = MedBillAmount - SettlementTotal;
                                txtBalance.Text = Balance.ToString("C");                                
                            }
                            else MessageBox.Show("Invalid Settlement Total format.", "Alert");
                        }
                    }
                    if (SettlementTotal > MedBillAmount)
                    {
                        MessageBox.Show("The total settlement amount exceeds Med Bill Amount.", "Alert");
                        gvSettlementsInMedBill.CurrentCell = gvSettlementsInMedBill.Rows[e.RowIndex].Cells[3];
                    }
                }

                if (e.ColumnIndex == 4)
                {
                    Decimal PersonalResponsibility = PersonalResponsibilityAmountInMedBill;
                    Decimal result = 0;
                    if ((gvSettlementsInMedBill["SettlementTypeValue", e.RowIndex].Value.ToString() == "Self Pay Discount")||
                        (gvSettlementsInMedBill["SettlementTypeValue", e.RowIndex].Value.ToString() == "3rd Party Discount")||
                        (gvSettlementsInMedBill["SettlementTypeValue", e.RowIndex].Value.ToString() == "Member Payment"))
                    {
                        result = 0;
                        Decimal SumPersonalResponsibility = 0;
                        for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                        {
                            if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                            {
                                SumPersonalResponsibility += result;
                            }
                        }

                        // Update txtPersonalResponsibility textbox
                        //PersonalResponsibility -= SumPersonalResponsibility;
                        //if (PersonalResponsibility < 0) txtPersonalResponsibility.BackColor = Color.Yellow;
                        //else if (PersonalResponsibility >= 0) txtPersonalResponsibility.BackColor = Color.White;
                        //txtPersonalResponsibility.Text = PersonalResponsibility.ToString("C");
                    }
                    else
                    {
                        MessageBox.Show("You have to select Self Pay Discount, 3rd Party Discount, or Member Payment to enter Personal Responsibility", "Alert");
                    }
                }

                if (e.ColumnIndex == 5)     // when one payment method is selected, clear other payment info
                {
                    if (gvSettlementsInMedBill["PaymentMethod", e.RowIndex]?.Value?.ToString() == "Check")
                    {

                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CheckNo"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill["ACHNo", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["ACHNo"].ReadOnly = true;

                        gvSettlementsInMedBill["CreditCard", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CreditCard"].ReadOnly = true;

                        gvSettlementsInMedBill.CurrentCell = gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CheckNo"];
                    }
                    if (gvSettlementsInMedBill["PaymentMethod", e.RowIndex]?.Value?.ToString() == "ACH/Banking")
                    {

                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["ACHNo"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill["CheckNo", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CheckNo"].ReadOnly = true;

                        gvSettlementsInMedBill["CreditCard", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CreditCard"].ReadOnly = true;

                        gvSettlementsInMedBill.CurrentCell = gvSettlementsInMedBill.Rows[e.RowIndex].Cells["ACHNo"];
                    }
                    if (gvSettlementsInMedBill["PaymentMethod", e.RowIndex]?.Value?.ToString() == "Credit Card")
                    {
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CreditCard"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["PaymentDate"].ReadOnly = false;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["Reconciled"].ReadOnly = false;

                        gvSettlementsInMedBill["CheckNo", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CheckNo"].ReadOnly = true;

                        gvSettlementsInMedBill["ACHNo", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["ACHNo"].ReadOnly = true;
 
                        gvSettlementsInMedBill.CurrentCell = gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CreditCard"];
                    }

                    if (gvSettlementsInMedBill[6, e.RowIndex]?.Value?.ToString() == "None")
                    {
                        gvSettlementsInMedBill["CheckNo", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CheckNo"].ReadOnly = true;

                        gvSettlementsInMedBill["ACHDate", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["ACHDate"].ReadOnly = true;

                        gvSettlementsInMedBill["CreditCard", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["CreditCard"].ReadOnly = true;

                        gvSettlementsInMedBill["PaymentDate", e.RowIndex].Value = String.Empty;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["PaymentDate"].ReadOnly = true;
                        gvSettlementsInMedBill["Reconciled", e.RowIndex].Value = 0;
                        gvSettlementsInMedBill.Rows[e.RowIndex].Cells["Reconciled"].ReadOnly = true;

                        gvSettlementsInMedBill.CurrentCell = gvSettlementsInMedBill.Rows[e.RowIndex].Cells["Note"];
                    }
                }

                if (e.ColumnIndex == 6)
                {
                    if ((Boolean)gvSettlementsInMedBill["Approved", e.RowIndex].Value == true)
                    {
                        CalendarCell approvedDateCell = new CalendarCell();
                        approvedDateCell.Value = DateTime.Today.ToString("MM/dd/yyyy");
                        gvSettlementsInMedBill["ApprovedDate", e.RowIndex] = approvedDateCell;
                    }
                    else
                    {
                        CalendarCell approvedDateCell = new CalendarCell();
                        approvedDateCell.Value = null;
                        gvSettlementsInMedBill["ApprovedDate", e.RowIndex] = approvedDateCell;
                    }
                }
            }
        }

        private void gvProcessingCaseNo_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (tbCMMManager.TabPages.Contains(tbpgCreateCase))
            {
                MessageBox.Show("The Case page is already opened", "Alert", MessageBoxButtons.OK);
                return;
            }
            else
            {
                DataGridView gvCaseUnderProgress = (DataGridView)sender;
                String IndividualId = String.Empty;
                //String CaseId = String.Empty;
                String IndividualName = String.Empty;

                int nRowSelected = 0;

                if (gvCaseUnderProgress.Rows.Count > 0)
                {
                    nRowSelected = gvCaseUnderProgress.CurrentCell.RowIndex;

                    IndividualId = txtIndividualID.Text.Trim();
                    CaseIdForCasePageMedBill = gvCaseUnderProgress["CaseIdForIndividual", nRowSelected].Value.ToString().Trim();
                    //CaseNameSelected = gvCaseUnderProgress["CaseIdForIndividual", nRowSelected].Value.ToString().Trim();
                    IndividualName = txtLastName.Text.Trim() + ", " + txtFirstName.Text.Trim() + " " + txtMiddleName.Text.Trim();

                    txtCreateCaseIndividualName.Text = IndividualName;

                    String strSqlQueryForCase = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[Contact_ID], [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[ModifiDate], " +
                                                "[dbo].[tbl_case].[CreateStaff], [dbo].[tbl_case].[ModifiStaff], [dbo].[tbl_case].[Case_status], " +
                                                "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Form_Destination_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Form_Destination_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Form_Destination_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], [dbo].[tbl_case].[MedRec_Form_Destination_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], [dbo].[tbl_case].[Unknown_Form_Destination_File_Name], [dbo].[tbl_case].[Unknown_Receiv_Date], " +
                                                "[dbo].[tbl_case].[Case_status], [dbo].[tbl_case].[Note] " +
                                                "from [dbo].[tbl_case] where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualID";

                    SqlCommand cmdQueryForCase = new SqlCommand(strSqlQueryForCase, connRN);
                    cmdQueryForCase.CommandType = CommandType.Text;

                    cmdQueryForCase.Parameters.AddWithValue("@CaseName", CaseIdForCasePageMedBill);
                    cmdQueryForCase.Parameters.AddWithValue("@IndividualID", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrCaseForIndividual = cmdQueryForCase.ExecuteReader();
                    if (rdrCaseForIndividual.HasRows)
                    {
                        rdrCaseForIndividual.Read();

                        txtCaseName.Text = rdrCaseForIndividual.GetString(0);
                        txtCaseIndividualID.Text = rdrCaseForIndividual.GetString(1);

                        // NPF Form
                        if (rdrCaseForIndividual.GetBoolean(7) == true) chkNPF_CaseCreationPage.Checked = true;
                        if (!rdrCaseForIndividual.IsDBNull(8)) txtNPFFormFilePath.Text = rdrCaseForIndividual.GetString(8);
                        if (!rdrCaseForIndividual.IsDBNull(9)) strNPFormFilePathDestination = rdrCaseForIndividual.GetString(9);
                        if (!rdrCaseForIndividual.IsDBNull(10)) txtNPFUploadDate.Text = rdrCaseForIndividual.GetDateTime(10).ToString("MM/dd/yyyy");

                        // IB Form
                        if (rdrCaseForIndividual.GetBoolean(11) == true) chkIB_CaseCreationPage.Checked = true;
                        if (!rdrCaseForIndividual.IsDBNull(12)) txtIBFilePath.Text = rdrCaseForIndividual.GetString(12);
                        if (!rdrCaseForIndividual.IsDBNull(13)) strIBFilePathDestination = rdrCaseForIndividual.GetString(13);
                        if (!rdrCaseForIndividual.IsDBNull(14)) txtIBUploadDate.Text = rdrCaseForIndividual.GetDateTime(14).ToString("MM/dd/yyyy");

                        // POP Form
                        if (rdrCaseForIndividual.GetBoolean(15) == true) chkPoP_CaseCreationPage.Checked = true;
                        if (!rdrCaseForIndividual.IsDBNull(16)) txtPopFilePath.Text = rdrCaseForIndividual.GetString(16);
                        if (!rdrCaseForIndividual.IsDBNull(17)) strPopFilePathDestination = rdrCaseForIndividual.GetString(17);
                        if (!rdrCaseForIndividual.IsDBNull(18)) txtPoPUploadDate.Text = rdrCaseForIndividual.GetDateTime(18).ToString("MM/dd/yyyy");

                        // Med Rec Form
                        if (rdrCaseForIndividual.GetBoolean(19) == true) chkMedicalRecordCaseCreationPage.Checked = true;
                        if (!rdrCaseForIndividual.IsDBNull(20)) txtMedicalRecordFilePath.Text = rdrCaseForIndividual.GetString(20);
                        if (!rdrCaseForIndividual.IsDBNull(21)) strMedRecordFilePathDestination = rdrCaseForIndividual.GetString(21);
                        if (!rdrCaseForIndividual.IsDBNull(22)) txtMRUploadDate.Text = rdrCaseForIndividual.GetDateTime(22).ToString("MM/dd/yyyy");

                        // Unknown Doc Form
                        if (rdrCaseForIndividual.GetBoolean(23) == true) chkOtherDocCaseCreationPage.Checked = true;
                        if (!rdrCaseForIndividual.IsDBNull(24)) txtOtherDocumentFilePath.Text = rdrCaseForIndividual.GetString(24);
                        if (!rdrCaseForIndividual.IsDBNull(25)) strUnknownDocFilePathDestination = rdrCaseForIndividual.GetString(25);
                        if (!rdrCaseForIndividual.IsDBNull(26)) txtOtherDocUploadDate.Text = rdrCaseForIndividual.GetDateTime(26).ToString("MM/dd/yyyy");

                        // Case status
                        if (rdrCaseForIndividual.GetBoolean(27) == true) txtCaseStatus.Text = "Complete and Ready";
                        else txtCaseStatus.Text = "Pending - Additional Documents required";

                        // Note
                        if (!rdrCaseForIndividual.IsDBNull(28)) txtNoteOnCase.Text = rdrCaseForIndividual.GetString(28);


                        // Individual Name

                        tbCMMManager.TabPages.Insert(3, tbpgCaseView);
                        tbCMMManager.TabPages.Insert(4, tbpgCreateCase);
                        tbCMMManager.SelectedIndex = 4;
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    //String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], " +
                    //                                     "[dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_medbill].[CreatedById], " +
                    //                                     "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_medbill].[LastModifiedById], " +
                    //                                     "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                    //                                     "from [dbo].[tbl_medbill] " +
                    //                                     "inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId] " +
                    //                                     "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

                    String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], " +
                                     "[dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                     "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                     "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                                     "from [dbo].[tbl_medbill] " +
                                     "inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId] " +
                                     "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id] " +
                                     "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id] " +
                                     "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";



                    SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillInCase, connRN);
                    cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

                    cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", CaseIdForCasePageMedBill);
                    cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", IndividualId);

                    SqlDependency dependencyMedBillInCase = new SqlDependency(cmdQueryForMedBillsInCase);
                    dependencyMedBillInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseChange);



                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrMedBillInCase = cmdQueryForMedBillsInCase.ExecuteReader();

                    gvCasePageMedBills.Rows.Clear();
                    if (rdrMedBillInCase.HasRows)
                    {
                        while (rdrMedBillInCase.Read())
                        {
                            DataGridViewRow row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                            row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                            gvCasePageMedBills.Rows.Add(row);
                        }
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();
                }
            }
        }

        private void OnMedBillsInCaseChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnMedBillsInCaseChange;

                UpdateGridViewMedBillsInCase();
            }
        }

        private void UpdateGridViewMedBillsInCase()
        {
            String IndividualId = txtIndividualID.Text.Trim();
            //String CaseId = gvProcessingCaseNo["CaseIdForIndividual", nRowSelected].Value.ToString().Trim();

            String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], " +
                 "[dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                 "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                 "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                 "from [dbo].[tbl_medbill] " +
                 "inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId] " +
                 "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id] " +
                 "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id] " +
                 "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

            SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillInCase, connRN);
            cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

            cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", CaseIdForCasePageMedBill);
            cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", IndividualId);

            SqlDependency dependencyMedBillInCase = new SqlDependency(cmdQueryForMedBillsInCase);
            dependencyMedBillInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrMedBillInCase = cmdQueryForMedBillsInCase.ExecuteReader();

            //gvCasePageMedBills.Rows.Clear();
            ClearMedBillInCaseSafely();
            if (rdrMedBillInCase.HasRows)
            {
                while (rdrMedBillInCase.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                    //gvCasePageMedBills.Rows.Add(row);
                    AddNewRowToMedBillInCaseSafely(row);
                }
            }

            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        private void gvCasePageMedBills_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (!tbCMMManager.TabPages.Contains(tbpgMedicalBill))
            {
                DataGridView gvMedBill = (DataGridView)sender;
                String IndividualId = String.Empty;
                String CaseNo = String.Empty;

                if (gvMedBill.Rows.Count > 0)
                {

                    String IndividualName = String.Empty;

                    String CaseNameInMedBill = txtCaseName.Text.Trim();
                    String IndividualIdInMedBill = txtCaseIndividualID.Text.Trim();

                    String MedBillNo = gvCasePageMedBills["MedBillNo", e.RowIndex].Value.ToString();

                    //////////////////////////////////////////////////////////////////////////////////
                    String strPatientLastName = txtLastName.Text.Trim();
                    String strPatientFirstName = txtFirstName.Text.Trim();
                    String strPatientMiddleName = txtMiddleName.Text.Trim();
                    String strDateOfBirth = dtpBirthDate.Value.ToString("MM/dd/yyyy");
                    String strSSN = txtIndividualSSN.Text.Trim();
                    String strStreetAddr = txtStreetAddress1.Text.Trim();
                    String strCity = txtCity1.Text.Trim();
                    String strState = txtState1.Text.Trim();
                    String strZip = txtZip1.Text.Trim();

                    txtIndividualIDMedBill.Text = IndividualIdInMedBill;
                    if (strPatientMiddleName.Trim() == String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName;
                    else if (strPatientMiddleName.Trim() != String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;

                    txtMedBillDOB.Text = strDateOfBirth;
                    txtMedBillSSN.Text = strSSN;
                    txtMedBillAddress.Text = strStreetAddr + ", " + strCity + ", " + strState + " " + strZip;

                    // populate Medical Bill types
                    String strSqlQueryForMedBillTypes = "select [dbo].[tbl_medbill_type].[MedBillTypeId], [dbo].[tbl_medbill_type].[MedBillTypeName] from [dbo].[tbl_medbill_type]";

                    SqlCommand cmdQueryForMedBillTypes = new SqlCommand(strSqlQueryForMedBillTypes, connRN);
                    cmdQueryForMedBillTypes.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrMedBillTypes = cmdQueryForMedBillTypes.ExecuteReader();
                    dicMedBillTypes.Clear();

                    if (rdrMedBillTypes.HasRows)
                    {
                        while (rdrMedBillTypes.Read())
                        {
                            if (!rdrMedBillTypes.IsDBNull(0) && !rdrMedBillTypes.IsDBNull(1))
                            {
                                dicMedBillTypes.Add(rdrMedBillTypes.GetInt16(0), rdrMedBillTypes.GetString(1));
                            }
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();



                    // Get the Medical Bill Note Type info
                    List<MedBillNoteTypeInfo> lstMedBillNoteTypeInfo = new List<MedBillNoteTypeInfo>();

                    String strSqlQueryForMedBillNoteTypeInfo = "select [dbo].[tbl_MedBillNoteType].[MedBillNoteTypeId], [dbo].[tbl_MedBillNoteType].[MedBillNoteTypeValue] from [dbo].[tbl_MedBillNoteType]";

                    SqlCommand cmdQueryForMedBillNoteTypeInfo = new SqlCommand(strSqlQueryForMedBillNoteTypeInfo, connRN);
                    cmdQueryForMedBillNoteTypeInfo.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();

                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrMedBillNoteType = cmdQueryForMedBillNoteTypeInfo.ExecuteReader();
                    if (rdrMedBillNoteType.HasRows)
                    {
                        while (rdrMedBillNoteType.Read())
                        {
                            if (!rdrMedBillNoteType.IsDBNull(0) && !rdrMedBillNoteType.IsDBNull(1))
                            {
                                lstMedBillNoteTypeInfo.Add(new MedBillNoteTypeInfo { MedBillNoteTypeId = rdrMedBillNoteType.GetInt16(0), MedBillNoteTypeValue = rdrMedBillNoteType.GetString(1) });
                            }
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    // Populate Pending Reason
                    comboPendingReason.Items.Clear();

                    if (dicPendingReason.Count > 0)
                    {
                        for (int i = 0; i < dicPendingReason.Count; i++)
                        {
                            comboPendingReason.Items.Add(dicPendingReason[i]);
                        }
                        comboPendingReason.SelectedIndex = 0;
                    }

                    // Populate Ineligible Reason
                    comboIneligibleReason.Items.Clear();

                    if (dicIneligibleReason.Count > 0)
                    {
                        for (int i = 0; i < dicIneligibleReason.Count; i++)
                        {
                            comboIneligibleReason.Items.Add(dicIneligibleReason[i]);
                        }
                        comboIneligibleReason.SelectedIndex = 0;
                    }

                    // Get medical bill info

                    String ICD10Code = String.Empty;


                    String strSqlQueryForMedBillEdit = "select [dbo].[tbl_medbill].[Case_Id], [dbo].[tbl_medbill].[Illness_Id], [dbo].[tbl_medbill].[Incident_Id], " +
                                                       "[dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill].[MedBillType_Id], [dbo].[tbl_medbill].[BillStatus], " +
                                                       "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_MedicalProvider].[Name], " +
                                                       "[dbo].[tbl_medbill].[PrescriptionDrugName], [dbo].[tbl_medbill].[PrescriptionNo], [dbo].[tbl_medbill].[PrescriptionDescription], " +
                                                       "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy], [dbo].[tbl_medbill].[PatientTypeId], " +
                                                       "[dbo].[tbl_medbill].[BillDate], [dbo].[tbl_medbill].[DueDate], " +
                                                       "[dbo].[tbl_medbill].[Account_At_Provider], [dbo].[tbl_MedicalProvider].[PHONE], [dbo].[tbl_medbill].[ProviderContactPerson], " +
                                                       "[dbo].[tbl_medbill].[Note], " +
                                                       "[dbo].[tbl_illness].[ICD_10_Id], " +
                                                       "[dbo].[tbl_medbill].[PendingReason], [dbo].[tbl_medbill].[IneligibleReason], " +
                                                       "[dbo].[tbl_medbill].[Account_At_Provider], [dbo].[tbl_medbill].[ProviderPhoneNumber], [dbo].[tbl_medbill].[ProviderContactPerson], " +
                                                       "[dbo].[tbl_medbill].[ProposalLetterSentDate], [dbo].[tbl_medbill].[HIPPASentDate], [dbo].[tbl_medbill].[MedicalRecordDate] " +
                                                       "from (([dbo].[tbl_medbill] inner join [dbo].[tbl_illness] on [dbo].[tbl_medbill].[Illness_Id] = [dbo].[tbl_illness].[Illness_Id]) " +
                                                       "inner join [dbo].[tbl_MedicalProvider] on [dbo].[tbl_medbill].[MedicalProvider_Id] = [dbo].[tbl_MedicalProvider].[ID]) " +
                                                       "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and " +
                                                       "[dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                       "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and" +
                                                       "[dbo].[tbl_medbill].[IsDeleted] = 0";

                    SqlCommand cmdQueryForMedBillEdit = new SqlCommand(strSqlQueryForMedBillEdit, connRN);
                    cmdQueryForMedBillEdit.CommandType = CommandType.Text;

                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@CaseName", CaseNameInMedBill);
                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrMedBillEdit = cmdQueryForMedBillEdit.ExecuteReader();
                    if (rdrMedBillEdit.HasRows)
                    {
                        rdrMedBillEdit.Read();

                        if (!rdrMedBillEdit.IsDBNull(0)) txtMedBill_CaseNo.Text = rdrMedBillEdit.GetString(0).Trim();
                        if (!rdrMedBillEdit.IsDBNull(1)) Illness.IllnessId = rdrMedBillEdit.GetString(1).Trim();
                        if (!rdrMedBillEdit.IsDBNull(2)) txtMedBill_Incident.Text = rdrMedBillEdit.GetString(2).Trim();

                        if (!rdrMedBillEdit.IsDBNull(3)) txtMedBillNo.Text = rdrMedBillEdit.GetString(3).Trim();

                        if (!rdrMedBillEdit.IsDBNull(4))
                        {
                            comboMedBillType.Items.Clear();
                            for (int i = 1; i <= dicMedBillTypes.Count; i++)
                            {
                                comboMedBillType.Items.Add(dicMedBillTypes[i]);
                            }
                            comboMedBillType.SelectedIndex = rdrMedBillEdit.GetInt16(4) - 1;
                        }

                        if (!rdrMedBillEdit.IsDBNull(5))
                        {
                            if (dicMedBillStatus.Count > 0)
                            {
                                comboMedBillStatus.Items.Clear();
                                for (int i = 0; i < dicMedBillStatus.Count; i++)
                                {
                                    comboMedBillStatus.Items.Add(dicMedBillStatus[i]);
                                }
                                comboMedBillStatus.SelectedIndex = rdrMedBillEdit.GetInt16(5);
                            }
                        }
                        if (!rdrMedBillEdit.IsDBNull(6))
                        {
                            txtMedBillAmount.Text = rdrMedBillEdit.GetDecimal(6).ToString("C");
                            txtBalance.Text = rdrMedBillEdit.GetDecimal(6).ToString("C");
                        }
                        if (!rdrMedBillEdit.IsDBNull(7)) txtMedicalProvider.Text = rdrMedBillEdit.GetString(7).Trim();
                        if (!rdrMedBillEdit.IsDBNull(8)) txtPrescriptionName.Text = rdrMedBillEdit.GetString(8).Trim();
                        if (!rdrMedBillEdit.IsDBNull(9)) txtNumberOfMedication.Text = rdrMedBillEdit.GetString(9).Trim();
                        if (!rdrMedBillEdit.IsDBNull(10)) txtPrescriptionDescription.Text = rdrMedBillEdit.GetString(10).Trim();
                        if (!rdrMedBillEdit.IsDBNull(11)) txtNumPhysicalTherapy.Text = rdrMedBillEdit.GetInt16(11).ToString();
                        if (!rdrMedBillEdit.IsDBNull(12))
                        {
                            int nPatientType = rdrMedBillEdit.GetInt16(12);

                            if (nPatientType == 0) rbOutpatient.Checked = true;
                            else if (nPatientType == 1) rbInpatient.Checked = true;
                        }
                        // Bill date
                        if (!rdrMedBillEdit.IsDBNull(13))
                        {
                            dtpBillDate.Text = rdrMedBillEdit.GetDateTime(13).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            dtpBillDate.Format = DateTimePickerFormat.Custom;
                            dtpBillDate.CustomFormat = " ";
                        }

                        // Due date
                        if (!rdrMedBillEdit.IsDBNull(14))
                        {
                            dtpDueDate.Text = rdrMedBillEdit.GetDateTime(14).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            dtpDueDate.Format = DateTimePickerFormat.Custom;
                            dtpDueDate.CustomFormat = " ";
                        }

                        if (!rdrMedBillEdit.IsDBNull(15)) txtMedBillAccountNoAtProvider.Text = rdrMedBillEdit.GetString(15);
                        if (!rdrMedBillEdit.IsDBNull(16)) txtMedProviderPhoneNo.Text = rdrMedBillEdit.GetString(16);
                        if (!rdrMedBillEdit.IsDBNull(17)) txtProviderContactPerson.Text = rdrMedBillEdit.GetString(17);

                        if (!rdrMedBillEdit.IsDBNull(18))
                        {
                            if (comboMedBillType.SelectedIndex == 0) txtMedBillNote.Text = rdrMedBillEdit.GetString(18);
                            if (comboMedBillType.SelectedIndex == 1) txtPrescriptionNote.Text = rdrMedBillEdit.GetString(18);
                            if (comboMedBillType.SelectedIndex == 2) txtPhysicalTherapyRxNote.Text = rdrMedBillEdit.GetString(18);
                        }
 
                        if (!rdrMedBillEdit.IsDBNull(19))
                        {
                            ICD10Code = rdrMedBillEdit.GetString(19).Trim();
                            Illness.ICD10Code = ICD10Code;
                            txtMedBill_Illness.Text = ICD10Code;
                        }

                        if ((comboMedBillType.SelectedIndex == 0) && (!rdrMedBillEdit.IsDBNull(20)))
                        {
                            comboPendingReason.SelectedIndex = rdrMedBillEdit.GetInt32(20);
                        }

                        if ((comboMedBillType.SelectedIndex == 0) && (!rdrMedBillEdit.IsDBNull(21)))
                        {
                            comboIneligibleReason.SelectedIndex = rdrMedBillEdit.GetInt32(21);
                        }

                        // Reset fields

                        if (comboMedBillType.SelectedIndex == 0)       // Medical Bill Type - Medical Bill
                        {
                            txtPrescriptionName.Text = String.Empty;
                            txtPrescriptionDescription.Text = String.Empty;
                            txtPrescriptionNote.Text = String.Empty;
                            txtNumberOfMedication.Text = String.Empty;

                            txtNumPhysicalTherapy.Text = String.Empty;
                            txtPhysicalTherapyRxNote.Text = String.Empty;
                        }
                        else if (comboMedBillType.SelectedIndex == 1)       // Medical Bill Type - Prescription
                        {
                            txtNumPhysicalTherapy.Text = String.Empty;
                            txtPhysicalTherapyRxNote.Text = String.Empty;

                            rbInpatient.Checked = false;
                            rbOutpatient.Checked = false;

                            comboPendingReason.SelectedIndex = 0;
                            comboIneligibleReason.SelectedIndex = 0;

                            txtMedBillNote.Text = String.Empty;
                        }
                        else if (comboMedBillType.SelectedIndex == 2)       // Medical Bill Type - Physical Therapy
                        {
                            txtPrescriptionName.Text = String.Empty;
                            txtPrescriptionDescription.Text = String.Empty;
                            txtPrescriptionNote.Text = String.Empty;
                            txtNumberOfMedication.Text = String.Empty;

                            rbInpatient.Checked = false;
                            rbOutpatient.Checked = false;

                            comboPendingReason.SelectedIndex = 0;
                            comboIneligibleReason.SelectedIndex = 0;

                            txtMedBillNote.Text = String.Empty;
                        }

                        if (!rdrMedBillEdit.IsDBNull(22)) txtMedBillAccountNoAtProvider.Text = rdrMedBillEdit.GetString(22);
                        if (!rdrMedBillEdit.IsDBNull(23)) txtMedProviderPhoneNo.Text = rdrMedBillEdit.GetString(23);
                        if (!rdrMedBillEdit.IsDBNull(24)) txtProviderContactPerson.Text = rdrMedBillEdit.GetString(24);

                        if (!rdrMedBillEdit.IsDBNull(25))
                        {
                            dtpProposalLetterSentDate.Value = rdrMedBillEdit.GetDateTime(25);
                            dtpProposalLetterSentDate.Format = DateTimePickerFormat.Short;
                        }
                        if (!rdrMedBillEdit.IsDBNull(26))
                        {
                            dtpHippaSentDate.Value = rdrMedBillEdit.GetDateTime(26);
                            dtpHippaSentDate.Format = DateTimePickerFormat.Short;
                        }
                        if (!rdrMedBillEdit.IsDBNull(27))
                        {
                            dtpMedicalRecordDate.Value = rdrMedBillEdit.GetDateTime(27);
                            dtpMedicalRecordDate.Format = DateTimePickerFormat.Short;
                        }

                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ///

                    //String IncidentNo = txtMedBill_Incident.Text.Trim();
                    //String IndividualIdMedBill = txtCaseIndividualID.Text.Trim();

                    //String strSqlQueryForIncidentChange = "select [cdc].[dbo_tbl_incident_CT].[Program_id], [dbo].[tbl_program].[ProgramName] from [cdc].[dbo_tbl_incident_CT] " +
                    //                                      "inner join [dbo].[tbl_program] on [cdc].[dbo_tbl_incident_CT].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                    //                                      "where [cdc].[dbo_tbl_incident_CT].[Incident_id] = @IncidentId and [cdc].[dbo_tbl_incident_CT].[Individual_id] = @IndividualId and " +
                    //                                      "([cdc].[dbo_tbl_incident_CT].[__$operation] = 2 or [cdc].[dbo_tbl_incident_CT].[__$operation] = 3 or " +
                    //                                      "[cdc].[dbo_tbl_incident_CT].[__$operation] = 4) " +      // capture incident program for insert, update
                    //                                      "order by [cdc].[dbo_tbl_incident_CT].[Program_id]";

                    //SqlCommand cmdQueryForIncidentChange = new SqlCommand(strSqlQueryForIncidentChange, connRN);
                    //cmdQueryForIncidentChange.CommandType = CommandType.Text;

                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IncidentId", IncidentNo);
                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);

                    //connRN.Open();
                    //SqlDataReader rdrIncidentChange = cmdQueryForIncidentChange.ExecuteReader();
                    //if (rdrIncidentChange.HasRows)
                    //{
                    //    while (rdrIncidentChange.Read())
                    //    {
                    //        //lstIncidentProgramInfo.Add(new IncidentProgramInfo { IncidentProgramId = rdrIncidentChange.GetInt16(0), IncidentProgramName = rdrIncidentChange.GetString(1).Trim() });
                    //        IncidentProgramInfo incidentProgram = new IncidentProgramInfo(rdrIncidentChange.GetInt16(0), rdrIncidentChange.GetString(1).Trim());
                    //        lstIncidentProgramInfo.Add(incidentProgram);
                    //    }
                    //}
                    //connRN.Close();

                    //Boolean bBronze = false;
                    //Boolean bSilver = false;
                    //Boolean bGold = false;
                    //Boolean bGoldPlus = false;
                    //Boolean bGoldMed1 = false;
                    //Boolean bGoldMed2 = false;

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.IncidentProgramId == 3)
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bBronze = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 2) && (bBronze == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bSilver = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 1) && (bBronze == false) && (bSilver == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 0) && (bBronze == false) && (bSilver == false) && (bGold == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 4) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 5) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false) && (bGoldMed1 == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.bPersonalResponsibilityProgram == true)
                    //        PersonalResponsibilityAmountInMedBill = incidentInfo.PersonalResponsibilityAmount;
                    //}

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    // Get disease name

                    if (ICD10Code != String.Empty)
                    {
                        String strSqlQueryForDiseaseName = "select [dbo].[ICD10 Code].[Name] from [dbo].[ICD10 Code] where [dbo].[ICD10 Code].[ICD10_CODE__C] = @ICD10Code";

                        SqlCommand cmdQueryForDiseaseName = new SqlCommand(strSqlQueryForDiseaseName, connSalesforce);
                        cmdQueryForDiseaseName.CommandType = CommandType.Text;

                        cmdQueryForDiseaseName.Parameters.AddWithValue("@ICD10Code", ICD10Code);

                        if (connSalesforce.State == ConnectionState.Open)
                        {
                            connSalesforce.Close();
                            connSalesforce.Open();
                        }
                        else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                        String DiseaseName = String.Empty;
                        Object objDiseaseName = cmdQueryForDiseaseName.ExecuteScalar();

                        if (objDiseaseName != null) DiseaseName = objDiseaseName.ToString();
                        else
                        {
                            MessageBox.Show("No Disease Name for ICD 10 Code: " + ICD10Code, "Error", MessageBoxButtons.OK);
                            return;
                        }

                        if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                        txtMedBill_ICD10Code.Text = ICD10Code;
                        txtMedBillDiseaseName.Text = DiseaseName;
                    }
                    else
                    {
                        txtMedBill_ICD10Code.Text = String.Empty;
                        txtMedBillDiseaseName.Text = String.Empty;
                    }
                    // Get documents info
                    String strSqlQueryForDocumentsInfo = "select [dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
                                                         "from [dbo].[tbl_case] where [dbo].[tbl_case].[Case_Name] = @CaseId and " +
                                                         "[dbo].[tbl_case].[Contact_ID] = @IndividualId and " +
                                                         "[dbo].[tbl_case].[IsDeleted] = 0";

                    SqlCommand cmdQueryForDocInfo = new SqlCommand(strSqlQueryForDocumentsInfo, connRN);
                    cmdQueryForDocInfo.CommandType = CommandType.Text;

                    cmdQueryForDocInfo.Parameters.AddWithValue("@CaseId", CaseNameInMedBill);
                    cmdQueryForDocInfo.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrDocInfo = cmdQueryForDocInfo.ExecuteReader();
                    if (rdrDocInfo.HasRows)
                    {
                        rdrDocInfo.Read();

                        if (!rdrDocInfo.IsDBNull(0))
                        {
                            if (rdrDocInfo.GetBoolean(0))
                            {
                                chkMedBillNPFReceived.Checked = true;
                                chkMedBillNPFReceived.Enabled = false;
                                dtpMedBillNPF.Text = rdrDocInfo.GetDateTime(1).ToString("MM/dd/yyyy");
                                dtpMedBillNPF.Enabled = false;
                                btnViewNPF.Enabled = true;
                            }
                            else
                            {
                                chkMedBillNPFReceived.Checked = false;
                                chkMedBillNPFReceived.Enabled = false;
                                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
                                dtpMedBillNPF.CustomFormat = " ";
                                dtpMedBillNPF.Enabled = false;
                                btnViewNPF.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(2))
                        {
                            if (rdrDocInfo.GetBoolean(2))
                            {
                                chkMedBill_IBReceived.Checked = true;
                                chkMedBill_IBReceived.Enabled = false;
                                dtpMedBill_IB.Text = rdrDocInfo.GetDateTime(3).ToString("MM/dd/yyyy");
                                dtpMedBill_IB.Enabled = false;
                                btnViewIB.Enabled = true;
                            }
                            else
                            {
                                chkMedBill_IBReceived.Checked = false;
                                chkMedBill_IBReceived.Enabled = false;
                                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
                                dtpMedBill_IB.CustomFormat = " ";
                                dtpMedBill_IB.Enabled = false;
                                btnViewIB.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(4))
                        {
                            if (rdrDocInfo.GetBoolean(4))
                            {
                                chkMedBillPOPReceived.Checked = true;
                                chkMedBillPOPReceived.Enabled = false;
                                dtpMedBillPOP.Text = rdrDocInfo.GetDateTime(5).ToString("MM/dd/yyyy");
                                dtpMedBillPOP.Enabled = false;
                                btnViewPoP.Enabled = true;
                            }
                            else
                            {
                                chkMedBillPOPReceived.Checked = false;
                                chkMedBillPOPReceived.Enabled = false;
                                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
                                dtpMedBillPOP.CustomFormat = " ";
                                dtpMedBillPOP.Enabled = false;
                                btnViewPoP.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(6))
                        {
                            if (rdrDocInfo.GetBoolean(6))
                            {
                                chkMedRecordReceived.Checked = true;
                                chkMedRecordReceived.Enabled = false;
                                dtpMedBillMedRecord.Text = rdrDocInfo.GetDateTime(7).ToString("MM/dd/yyyy");
                                dtpMedBillMedRecord.Enabled = false;
                                btnViewMedRecord.Enabled = true;
                            }
                            else
                            {
                                chkMedRecordReceived.Checked = false;
                                chkMedRecordReceived.Enabled = false;
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
                                dtpMedBillMedRecord.CustomFormat = " ";
                                dtpMedBillMedRecord.Enabled = false;
                                btnViewMedRecord.Enabled = false;
                            }
                        }

                        if (!rdrDocInfo.IsDBNull(8))
                        {
                            if (rdrDocInfo.GetBoolean(8))
                            {
                                chkOtherDocReceived.Checked = true;
                                chkOtherDocReceived.Enabled = false;
                                dtpMedBillOtherDoc.Text = rdrDocInfo.GetDateTime(9).ToString("MM/dd/yyyy");
                                dtpMedBillOtherDoc.Enabled = false;
                                btnViewOtherDoc.Enabled = true;
                            }
                            else
                            {
                                chkOtherDocReceived.Checked = false;
                                chkOtherDocReceived.Enabled = false;
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
                                dtpMedBillOtherDoc.CustomFormat = " ";
                                dtpMedBillOtherDoc.Enabled = false;
                                btnViewOtherDoc.Enabled = false;
                            }
                        }

                        strCaseIdSelected = CaseNameInMedBill;
                        strContactIdSelected = IndividualIdInMedBill;
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    //String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] where [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                    //SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                    //cmdQueryForIllnessId.CommandType = CommandType.Text;

                    //cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", Illness.ICD10Code);

                    //connRN.Open();
                    //Illness.IllnessId = cmdQueryForIllnessId.ExecuteScalar().ToString();
                    //connRN.Close();

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ///

                    String strSqlQueryForIncidentProgram = "select [dbo].[tbl_program].[ProgramName] from [dbo].[tbl_program] inner join [dbo].[tbl_incident] " +
                                       "on [dbo].[tbl_program].[Program_id] = [dbo].[tbl_incident].[Program_id] " +
                                       "where [dbo].[tbl_incident].[Individual_id] = @IndividualId and [dbo].[tbl_incident].[Incident_id] = @IncidentId";

                    SqlCommand cmdQueryForIncidentProgram = new SqlCommand(strSqlQueryForIncidentProgram, connRN);
                    cmdQueryForIncidentProgram.CommandType = CommandType.Text;

                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);
                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IncidentId", txtMedBill_Incident.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String IncidentProgramName = cmdQueryForIncidentProgram.ExecuteScalar()?.ToString();
                    Object objIncidentProgramName = cmdQueryForIncidentProgram.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String IncidentProgramName = String.Empty;
                    if (objIncidentProgramName != null) IncidentProgramName = objIncidentProgramName.ToString();
                    else
                    {
                        MessageBox.Show("No Program Name for Incident Id: " + txtMedBill_Incident.Text.Trim(), "Error", MessageBoxButtons.OK);
                        return;
                    }

                    if (IncidentProgramName != String.Empty) txtIncdProgram.Text = IncidentProgramName;

                    if (txtIncdProgram.Text.Trim() != txtMemberProgram.Text.Trim())
                    {
                        txtIncdProgram.BackColor = Color.Red;
                        txtMemberProgram.BackColor = Color.Red;
                    }
                    else if (txtIncdProgram.Text.Trim() == txtMemberProgram.Text.Trim())
                    {
                        txtIncdProgram.BackColor = Color.White;
                        txtMemberProgram.BackColor = Color.FromKnownColor(KnownColor.Control);
                    }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        String strSqlQueryForMedicalProvider = "select dbo.tbl_MedicalProvider.ID, dbo.tbl_MedicalProvider.Name, dbo.tbl_MedicalProvider.Type from dbo.tbl_MedicalProvider";

                    SqlCommand cmdQueryForMedicalProvider = new SqlCommand(strSqlQueryForMedicalProvider, connRN);
                    cmdQueryForMedicalProvider.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();

                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrMedicalProvider = cmdQueryForMedicalProvider.ExecuteReader();

                    lstMedicalProvider.Clear();
                    if (rdrMedicalProvider.HasRows)
                    {
                        while (rdrMedicalProvider.Read())
                        {
                            MedicalProviderInfo info = new MedicalProviderInfo();

                            if (!rdrMedicalProvider.IsDBNull(0)) info.ID = rdrMedicalProvider.GetString(0);
                            if (!rdrMedicalProvider.IsDBNull(1)) info.Name = rdrMedicalProvider.GetString(1);
                            if (!rdrMedicalProvider.IsDBNull(2)) info.Type = rdrMedicalProvider.GetString(2);

                            lstMedicalProvider.Add(info);
                        }
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    var srcMedicalProvider = new AutoCompleteStringCollection();

                    for (int i = 0; i < lstMedicalProvider.Count; i++)
                    {
                        srcMedicalProvider.Add(lstMedicalProvider[i].Name);
                    }

                    txtMedicalProvider.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtMedicalProvider.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtMedicalProvider.AutoCompleteCustomSource = srcMedicalProvider;

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    int nRowSelected = gvMedBill.CurrentCell.RowIndex;

                    IndividualId = txtCaseIndividualID.Text.Trim();
                    CaseNo = txtCaseName.Text.Trim();
                    MedicalBillNo = gvMedBill[1, nRowSelected].Value.ToString();

 
                    lstPaymentMethod.Clear();
                    String strSqlQueryForPaymentMethod = "select [dbo].[tbl_payment_method].[PaymentMethod_Id], [dbo].[tbl_payment_method].[PaymentMethod_Value] from [dbo].[tbl_payment_method] " +
                                                         "order by [dbo].[tbl_payment_method].[PaymentMethod_Value]";

                    SqlCommand cmdQueryForPaymentMethod = new SqlCommand(strSqlQueryForPaymentMethod, connRN);
                    cmdQueryForPaymentMethod.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrPaymentMethod = cmdQueryForPaymentMethod.ExecuteReader();
                    if (rdrPaymentMethod.HasRows)
                    {
                        while (rdrPaymentMethod.Read())
                        {
                            if (!rdrPaymentMethod.IsDBNull(1)) lstPaymentMethod.Add(new PaymentMethod { PaymentMethodId = rdrPaymentMethod.GetInt16(0), PaymentMethodValue = rdrPaymentMethod.GetString(1) });
                            else lstPaymentMethod.Add(new PaymentMethod { PaymentMethodId = rdrPaymentMethod.GetInt16(0), PaymentMethodValue = null });
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    // Retrieve credit card info
                    lstCreditCardInfo.Clear();
                    String strSqlQueryForCreditCardInfo = "select [dbo].[tbl_Credit_Card__c].[CreditCard_Id], [dbo].[tbl_Credit_Card__c].[Name] from [dbo].[tbl_Credit_Card__c]";

                    SqlCommand cmdQueryForCreditCardInfo = new SqlCommand(strSqlQueryForCreditCardInfo, connRN);
                    cmdQueryForCreditCardInfo.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrCreditCardInfo = cmdQueryForCreditCardInfo.ExecuteReader();
                    if (rdrCreditCardInfo.HasRows)
                    {
                        while (rdrCreditCardInfo.Read())
                        {
                            if (!rdrCreditCardInfo.IsDBNull(1))
                                lstCreditCardInfo.Add(new CreditCardInfo { CreditCardId = rdrCreditCardInfo.GetInt16(0), CreditCardNo = rdrCreditCardInfo.GetString(1) });
                            else
                                lstCreditCardInfo.Add(new CreditCardInfo { CreditCardId = rdrCreditCardInfo.GetInt16(0), CreditCardNo = null });
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String strSqlQueryForSettlement = "select [dbo].[tbl_settlement].[Name], [dbo].[tbl_settlement_type_code].[SettlementTypeValue], [dbo].[tbl_settlement].[Amount], " +
                                                      "[dbo].[tbl_settlement].[PersonalResponsibilityCredit], [dbo].[tbl_payment_method].[PaymentMethod_Value], " +
                                                      "[dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate], " +
                                                      "[dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
                                                      "[dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], [dbo].[tbl_settlement].[ACH_Reconciled], " +
                                                      "[dbo].[tbl_Credit_Card__c].[Name], [dbo].[tbl_settlement].[CMMCreditCardPaidDate], [dbo].[tbl_settlement].[CC_Reconciled], " +
                                                      "[dbo].[tbl_settlement].[AllowedAmount], [dbo].[tbl_settlement].[IneligibleReason], " +
                                                      "[dbo].[tbl_settlement].[Notes] " +
                                                      "from [dbo].[tbl_settlement] inner join [dbo].[tbl_settlement_type_code] " +
                                                      "on [dbo].[tbl_settlement].[SettlementType] = [dbo].[tbl_settlement_type_code].[SettlementTypeCode] " +
                                                      "inner join [dbo].[tbl_payment_method] on [dbo].[tbl_settlement].[CMMPaymentMethod] = [dbo].[tbl_payment_method].[PaymentMethod_Id] " +
                                                      "inner join [dbo].[tbl_Credit_Card__c] on [dbo].[tbl_settlement].[CMMCreditCard] = [dbo].[tbl_Credit_Card__c].[CreditCard_Id]" +
                                                      "where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillNo and " +
                                                      "[dbo].[tbl_settlement].[IsDeleted] = 0 " +
                                                      "order by [dbo].[tbl_settlement].[Name]";

                    SqlCommand cmdQueryForSettlement = new SqlCommand(strSqlQueryForSettlement, connRN);
                    cmdQueryForSettlement.CommandType = CommandType.Text;

                    cmdQueryForSettlement.Parameters.AddWithValue("@MedBillNo", MedBillNo);

                    SqlDependency dependencySettlementInMedBill = new SqlDependency(cmdQueryForSettlement);
                    dependencySettlementInMedBill.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillEditChange);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrSettlement = cmdQueryForSettlement.ExecuteReader();
                    gvSettlementsInMedBill.Rows.Clear();
                    if (rdrSettlement.HasRows)
                    {
                        while (rdrSettlement.Read())
                        {
                            DataGridViewRow row = new DataGridViewRow();
                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                            if (!rdrSettlement.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(0) });
                            if (!rdrSettlement.IsDBNull(1))
                            {
                                DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstSettlementType.Count; i++)
                                {
                                    comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                                }
                                for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                        comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                                }

                                row.Cells.Add(comboCellSettlementType);
                            }
                            else
                            {
                                DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstSettlementType.Count; i++)
                                {
                                    comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                                }

                                for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                        comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                                }

                                row.Cells.Add(comboCellSettlementType);
                            }

                            if (!rdrSettlement.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(2).ToString("C") });
                            else
                            {
                                Decimal Zero = 0;
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                            }


                            if (!rdrSettlement.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(3).ToString("C") });
                            else
                            {
                                Decimal Zero = 0;
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                            }

                            /////////////////////////////////////////////////////////////////////
                            if (!rdrSettlement.IsDBNull(4))
                            {
                                DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstPaymentMethod.Count; i++)
                                {
                                    if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                    else comboCellPaymentMethod.Items.Add(String.Empty);
                                }

                                for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(4) == comboCellPaymentMethod.Items[i].ToString())
                                        comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
                                }

                                row.Cells.Add(comboCellPaymentMethod);
                            }
                            else
                            {
                                DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstPaymentMethod.Count; i++)
                                {
                                    if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                    else comboCellPaymentMethod.Items.Add(String.Empty);
                                    //comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                }

                                for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
                                {
                                    if ((!rdrSettlement.IsDBNull(4)) && comboCellPaymentMethod.Items[i] != null)
                                    {
                                        if (rdrSettlement.GetString(4) == comboCellPaymentMethod.Items[i].ToString())
                                            comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
                                    }
                                    else comboCellPaymentMethod.Value = null;
                                }

                                row.Cells.Add(comboCellPaymentMethod);

                            }

                            /////////////////////////////////////////////////////////////////////
                            if (!rdrSettlement.IsDBNull(5))
                            {

                                DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                                approvedCell.Value = rdrSettlement.GetBoolean(5);
                                approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                row.Cells.Add(approvedCell);
                            }
                            else
                            {
                                DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                                approvedCell.Value = false;
                                approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                row.Cells.Add(approvedCell);
                            }

                            if (!rdrSettlement.IsDBNull(6)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(6).ToString("MM/dd/yyyy")) });
                            else row.Cells.Add(new CalendarCell { Value = null });

                            // Payment information
                            if (!rdrSettlement.IsDBNull(4))
                            {
                                String strPaymentMethod = rdrSettlement.GetString(4);

                                switch (strPaymentMethod)
                                {
                                    case "Check":
                                        if (!rdrSettlement.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(7) });
                                        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNoneForCheck = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCardNoneForCheck.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNoneForCheck.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNoneForCheck);
                                        if (!rdrSettlement.IsDBNull(8)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(8).ToString("MM/dd/yyyy")) });
                                        if (!rdrSettlement.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(9) });
                                        break;
                                    case "ACH/Banking":
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        if (!rdrSettlement.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(10) });
                                        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNoneForACH = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null)
                                                comboCellCreditCardNoneForACH.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNoneForACH.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNoneForACH);
                                        if (!rdrSettlement.IsDBNull(11)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(11).ToString("MM/dd/yyyy")) });
                                        if (!rdrSettlement.IsDBNull(12)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(12) });
                                        break;
                                    case "Credit Card":
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                        if (!rdrSettlement.IsDBNull(13))
                                        {
                                            for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                            {
                                                if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                                else comboCellCreditCard.Items.Add(String.Empty);
                                            }
                                            for (int i = 0; i < comboCellCreditCard.Items.Count; i++)
                                            {
                                                if (rdrSettlement.GetString(13) == comboCellCreditCard.Items[i].ToString())
                                                    comboCellCreditCard.Value = comboCellCreditCard.Items[i];
                                            }
                                        }
                                        else
                                        {
                                            //DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                            for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                            {
                                                if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                                else comboCellCreditCard.Items.Add(String.Empty);
                                            }
                                            comboCellCreditCard.Value = String.Empty;
                                            //row.Cells.Add(comboCellCreditCard);
                                        }
                                        row.Cells.Add(comboCellCreditCard);
                                        if (!rdrSettlement.IsDBNull(14)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(14).ToString("MM/dd/yyyy")) });
                                        else row.Cells.Add(new CalendarCell { Value = String.Empty });
                                        if (!rdrSettlement.IsDBNull(15)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(15) });
                                        else row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                                        break;
                                    default:
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNone = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCardNone.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNone.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNone);
                                        row.Cells.Add(new CalendarCell { Value = null });
                                        row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });
                                        break;
                                }
                            }
                            else
                            {

                                DataGridViewTextBoxCell txtCheckNoCell = new DataGridViewTextBoxCell();
                                txtCheckNoCell.Value = null;
                                row.Cells.Add(txtCheckNoCell);
                                DataGridViewTextBoxCell txtACHNoCell = new DataGridViewTextBoxCell();
                                txtACHNoCell.Value = null;
                                row.Cells.Add(txtACHNoCell);
                                DataGridViewComboBoxCell comboCreditCardCell = new DataGridViewComboBoxCell();
                                for(int i = 0; i < lstCreditCardInfo.Count; i++)
                                {
                                    if (lstCreditCardInfo[i].CreditCardNo != null) comboCreditCardCell.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                    else comboCreditCardCell.Items.Add(String.Empty);
                                }
                                row.Cells.Add(comboCreditCardCell);
                                comboCreditCardCell.ReadOnly = true;
                                CalendarCell calPaymentDate = new CalendarCell();
                                calPaymentDate.Value = null;
                                row.Cells.Add(calPaymentDate);
                                DataGridViewCheckBoxCell chkReconciledCell = new DataGridViewCheckBoxCell();
                                chkReconciledCell.Value = false;
                                row.Cells.Add(chkReconciledCell);

                            }


                            if (!rdrSettlement.IsDBNull(16)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(16).ToString("C") });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                            if (!rdrSettlement.IsDBNull(17))
                            {
                                if (dicIneligibleReason.Count > 0)
                                {
                                    DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < dicIneligibleReason.Count; i++)
                                    {
                                        comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                                    }
                                    comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[rdrSettlement.GetInt32(17)];
                                    row.Cells.Add(comboCellIneligibleReason);
                                }
                            }
                            else
                            {
                                if (dicIneligibleReason.Count > 0)
                                {
                                    DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < dicIneligibleReason.Count; i++)
                                    {
                                        comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                                    }
                                    comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[0];
                                    row.Cells.Add(comboCellIneligibleReason);
                                }
                            }

                            if (!rdrSettlement.IsDBNull(18)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(18) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                            gvSettlementsInMedBill.Rows.Add(row);
                            //AddNewRowToGVSettlementSafely(row);
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ///
                    //Decimal PersonalResponsibilityAmount = 0;

                    //foreach (IncidentProgramInfo incdProgram in lstIncidentProgramInfo)
                    //{
                    //    if (incdProgram.bPersonalResponsibilityProgram == true) PersonalResponsibilityAmount = incdProgram.PersonalResponsibilityAmount;
                    //}

                    //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    //{
                    //    if (gvSettlementsInMedBill["PersonalResponsibility", i]?.Value != null)
                    //    {
                    //        Decimal result = 0;
                    //        if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                    //        {
                    //            PersonalResponsibilityAmount -= result;
                    //        }
                    //    }
                    //}

                    //txtPersonalResponsibility.Text = PersonalResponsibilityAmount.ToString("C");

                    //if (PersonalResponsibilityAmount < 0) txtPersonalResponsibility.BackColor = Color.Yellow;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Check")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                        }

                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "ACH/Banking")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                        }

                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Credit Card")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                        }
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value == null)
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = true;
                        }
                    }

                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if (gvSettlementsInMedBill[2, i]?.Value?.ToString() == "Ineligible") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                        else
                        {
                            gvSettlementsInMedBill["IneligibleReason", i].Value = null;
                            gvSettlementsInMedBill["IneligibleReason", i].ReadOnly = true;
                        }
                    }

                    if (txtMedBillAmount.Text.Trim() != String.Empty)
                    {
                        Decimal SettlementTotal = 0;
                        Decimal Balance = 0;
                        Decimal Result = 0;
                        Decimal BillAmount = 0;
                        if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out Result))
                        {
                            BillAmount = Result;

                            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                            {
                                Decimal Settlement = Decimal.Parse(gvSettlementsInMedBill["SettlementAmount", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                                SettlementTotal += Settlement;
                            }
                            if (SettlementTotal > BillAmount) MessageBox.Show("Settlement Total exceeds the Medical Bill Amount.", "Alert");
                            else
                            {
                                Balance = BillAmount - SettlementTotal;
                                txtBalance.Text = Balance.ToString("C");
                            }
                        }
                    }

                    btnAddNewSettlement.Enabled = true;
                    //btnEditSettlement.Enabled = true;
                    btnSaveSettlement.Enabled = true;
                    btnDeleteSettlement.Enabled = true;

                    tbCMMManager.TabPages.Insert(5, tbpgMedicalBill);
                    tbCMMManager.SelectedIndex = 5;
                }
            }
            else
            {
                MessageBox.Show("Medical Bill page already open");
            }
        }

        //private void AttachControlEventHandlers()
        //{
            //txtMedBillGuarantor.TextChanged += new EventHandler(txtMedBillGuarantor_TextChanged);
            //txtMedBill_Illness.TextChanged += new EventHandler(txtMedBill_Illness_TextChanged);
            //txtMedBill_Incident.TextChanged += new EventHandler(txtMedBill_Incident_TextChanged);
            //txtMedBillAmount.TextChanged += new EventHandler(txtMedBillAmount_TextChanged);
            //txtBalance.TextChanged += new EventHandler(txtBalance_TextChanged);
            //txtPrescriptionName.TextChanged += new EventHandler(txtPrescriptionName_TextChanged);
            //txtPrescriptionNo.TextChanged += new EventHandler(txtPrescriptionNo_TextChanged);
            //txtPrescriptionDescription.TextChanged += new EventHandler(txtPrescriptionDescription_TextChanged);
            //txtNumPhysicalTherapy.TextChanged += new EventHandler(txtNumPhysicalTherapy_TextChanged);
            //cbMedicalBillNote1.SelectedIndexChanged += new EventHandler(cbMedicalBillNote1_SelectedIndexChanged);
            //cbMedicalBillNote2.SelectedIndexChanged += new EventHandler(cbMedicalBillNote2_SelectedIndexChanged);
            //cbMedicalBillNote3.SelectedIndexChanged += new EventHandler(cbMedicalBillNote3_SelectedIndexChanged);
            //cbMedicalBillNote4.SelectedIndexChanged += new EventHandler(cbMedicalBillNote4_SelectedIndexChanged);
            //txtMedicalBillNote1.TextChanged += new EventHandler(txtMedicalBillNote1_TextChanged);
            //txtMedicalBillNote2.TextChanged += new EventHandler(txtMedicalBillNote2_TextChanged);
            //txtMedicalBillNote3.TextChanged += new EventHandler(txtMedicalBillNote3_TextChanged);
            //txtMedicalBillNote4.TextChanged += new EventHandler(txtMedicalBillNote4_TextChanged);


            //dtpBillDate.ValueChanged += new EventHandler(dtpBillDate_ValueChanged);
            //dtpDueDate.ValueChanged += new EventHandler(dtpDueDate_ValueChanged);
        //}

        //private void DetachControlEventHandlers()
        //{
            //txtMedBillGuarantor.TextChanged -= txtMedBillGuarantor_TextChanged;
            //txtMedBill_Illness.TextChanged -= txtMedBill_Illness_TextChanged;
            //txtMedBill_Incident.TextChanged -= txtMedBill_Incident_TextChanged;
            //txtMedBillAmount.TextChanged -= txtMedBillAmount_TextChanged;
            //txtBalance.TextChanged -= txtBalance_TextChanged;
            //txtPrescriptionName.TextChanged -= txtPrescriptionName_TextChanged;
            //txtPrescriptionNo.TextChanged -= txtPrescriptionNo_TextChanged;
            //txtPrescriptionDescription.TextChanged -= txtPrescriptionDescription_TextChanged;
            //txtNumPhysicalTherapy.TextChanged -= txtNumPhysicalTherapy_TextChanged;
            //cbMedicalBillNote1.SelectedIndexChanged -= cbMedicalBillNote1_SelectedIndexChanged;
            //cbMedicalBillNote2.SelectedIndexChanged -= cbMedicalBillNote2_SelectedIndexChanged;
            //cbMedicalBillNote3.SelectedIndexChanged -= cbMedicalBillNote3_SelectedIndexChanged;
            //cbMedicalBillNote4.SelectedIndexChanged -= cbMedicalBillNote4_SelectedIndexChanged;
            //txtMedicalBillNote1.TextChanged -= txtMedicalBillNote1_TextChanged;
            //txtMedicalBillNote2.TextChanged -= txtMedicalBillNote2_TextChanged;
            //txtMedicalBillNote3.TextChanged -= txtMedicalBillNote3_TextChanged;
            //txtMedicalBillNote4.TextChanged -= txtMedicalBillNote4_TextChanged;


            //dtpBillDate.ValueChanged -= dtpBillDate_ValueChanged;
            //dtpDueDate.ValueChanged -= dtpDueDate_ValueChanged;
        //}

        

        private void gvCaseViewCaseHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView gvCaseHistory = (DataGridView)sender;

            //DataGridViewCheckBoxColumn chkSelected = gvCaseHistory[0, 0] as DataGridViewCheckBoxColumn;
            for (int i = 0; i < gvCaseHistory.Rows.Count; i++)
            {

            }
        }

        private void btnEditCase_Click(object sender, EventArgs e)
        {
            if (!tbCMMManager.TabPages.Contains(tbpgCreateCase))
            {
                caseMode = CaseMode.Edit;

                IndividualIdSelected = txtCaseHistoryIndividualID.Text.Trim();
                String IndividualName = txtCaseHistoryIndividualName.Text.Trim();
                int nRowSelected = 0;

                int nNumberOfRowSelected = 0;
                //Boolean bSelected = false;

                for (int i = 0; i < gvCaseViewCaseHistory.Rows.Count; i++)
                {
                    if ((Boolean)gvCaseViewCaseHistory[0, i].Value)
                    {
                        nNumberOfRowSelected++;
                        nRowSelected = i;
                    }
                }

                if (nNumberOfRowSelected == 0)
                {
                    MessageBox.Show("Please select a case.");
                }
                else if (nNumberOfRowSelected >= 2)
                {
                    MessageBox.Show("More than one row selected");
                }
                else if (nNumberOfRowSelected == 1)
                {

                    CaseIdSelected = gvCaseViewCaseHistory[1, nRowSelected].Value.ToString();
                    strCaseIdForIllness = CaseIdSelected;

                    //String strSqlQueryForCaseInfoSelected = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[Contact_ID], " +
                    //                                        "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Form_Destination_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                    //                                        "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Form_Destination_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
                    //                                        "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Form_Destination_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
                    //                                        "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], " +
                    //                                        "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                    //                                        "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], " +
                    //                                        "[dbo].[tbl_case].[Unknown_Form_Destination_File_Name], [dbo].[tbl_case].[Unknown_Receiv_Date], " +
                    //                                        "[dbo].[tbl_case].[Case_status], [dbo].[tbl_case].[Note] " +
                    //                                        "from [dbo].[tbl_case] " +
                    //                                        "where [dbo].[tbl_case].[Case_Name] = @CaseId and [dbo].[tbl_case].[Contact_ID] = @IndividualID";

                    String strSqlQueryForCaseInfoSelected = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[Contact_ID], " +
                                                            "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                            "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                            "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                            "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                            "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], [dbo].[tbl_case].[Unknown_Receiv_Date], " +
                                                            "[dbo].[tbl_case].[Case_status], [dbo].[tbl_case].[Note] " +
                                                            "from [dbo].[tbl_case] " +
                                                            "where [dbo].[tbl_case].[IsDeleted] = 0 and " +
                                                            "[dbo].[tbl_case].[Case_Name] = @CaseId and " +
                                                            "[dbo].[tbl_case].[Contact_ID] = @IndividualID";

                    SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfoSelected, connRN);
                    cmdQueryForCaseInfo.CommandType = CommandType.Text;

                    cmdQueryForCaseInfo.Parameters.AddWithValue("@CaseId", CaseIdSelected);
                    cmdQueryForCaseInfo.Parameters.AddWithValue("@IndividualID", IndividualIdSelected);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrCaseInfo = cmdQueryForCaseInfo.ExecuteReader();

                    if (rdrCaseInfo.HasRows)
                    {
                        rdrCaseInfo.Read();

                        // Populate NPF Form Info
                        if (!rdrCaseInfo.IsDBNull(0)) txtCaseName.Text = rdrCaseInfo.GetString(0);
                        if (!rdrCaseInfo.IsDBNull(1)) txtCaseIndividualID.Text = rdrCaseInfo.GetString(1);

                        if (txtMiddleName.Text == String.Empty) txtCreateCaseIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text;
                        else txtCreateCaseIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text + " " + txtMiddleName.Text;

                        if (!rdrCaseInfo.IsDBNull(2))
                        {
                            chkNPF_CaseCreationPage.Checked = (Boolean)rdrCaseInfo.GetBoolean(2);
                        }
                        if (!rdrCaseInfo.IsDBNull(3)) txtNPFFormFilePath.Text = rdrCaseInfo.GetString(3);
                        if (!rdrCaseInfo.IsDBNull(4)) txtNPFUploadDate.Text = rdrCaseInfo.GetDateTime(4).ToString("MM/dd/yyyy");

                        // Populate IB Form Info
                        if (!rdrCaseInfo.IsDBNull(5))
                        {
                            chkIB_CaseCreationPage.Checked = (Boolean)rdrCaseInfo.GetBoolean(5);
                        }
                        if (!rdrCaseInfo.IsDBNull(6)) txtIBFilePath.Text = rdrCaseInfo.GetString(6);
                        if (!rdrCaseInfo.IsDBNull(7)) txtIBUploadDate.Text = rdrCaseInfo.GetDateTime(7).ToString("MM/dd/yyyy");

                        // Populate POP Form Info
                        if (!rdrCaseInfo.IsDBNull(8))
                        {
                            chkPoP_CaseCreationPage.Checked = (Boolean)rdrCaseInfo.GetBoolean(8);
                        }
                        if (!rdrCaseInfo.IsDBNull(9)) txtPopFilePath.Text = rdrCaseInfo.GetString(9);
                        if (!rdrCaseInfo.IsDBNull(10)) txtPoPUploadDate.Text = rdrCaseInfo.GetDateTime(10).ToString("MM/dd/yyyy");

                        // Populate Med Rec Form Info
                        if (!rdrCaseInfo.IsDBNull(11))
                        {
                            chkMedicalRecordCaseCreationPage.Checked = (Boolean)rdrCaseInfo.GetBoolean(11);
                        }
                        if (!rdrCaseInfo.IsDBNull(12)) txtMedicalRecordFilePath.Text = rdrCaseInfo.GetString(12);
                        if (!rdrCaseInfo.IsDBNull(13)) txtMRUploadDate.Text = rdrCaseInfo.GetDateTime(13).ToString("MM/dd/yyyy");

                        // Populate Unknown Doc Info
                        if (!rdrCaseInfo.IsDBNull(14))
                        {
                            chkOtherDocCaseCreationPage.Checked = (Boolean)rdrCaseInfo.GetBoolean(14);
                        }
                        if (!rdrCaseInfo.IsDBNull(15)) txtOtherDocumentFilePath.Text = rdrCaseInfo.GetString(15);
                        if (!rdrCaseInfo.IsDBNull(16)) txtOtherDocUploadDate.Text = rdrCaseInfo.GetDateTime(16).ToString("MM/dd/yyyy");

                        // Populate case status
                        if (!rdrCaseInfo.IsDBNull(17))
                        {
                            if (rdrCaseInfo.GetBoolean(17))
                            {
                                txtCaseStatus.Text = "Complete and Ready";
                            }
                            else
                            {
                                txtCaseStatus.Text = "Pending - Additional Documents required";
                            }
                        }

                        // Populate Note
                        if (!rdrCaseInfo.IsDBNull(18))
                        {
                            txtNoteOnCase.Text = rdrCaseInfo.GetString(18);
                        }

                        // Populate note
                        if (!rdrCaseInfo.IsDBNull(18)) txtNoteOnCase.Text = rdrCaseInfo.GetString(18);
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    // Populate the gvCasePageMedBills with Med Bill in the case
                    String strSqlQueryForMedBillsInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                                          "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                          "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                                                          "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                          "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                          "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                          "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                          "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                          "[dbo].[tbl_medbill].[IsDeleted] = 0";

                    SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillsInCase, connRN);
                    cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

                    cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", CaseIdSelected);
                    cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", IndividualIdSelected);

                    SqlDependency dependencyMedBillsInCaseEdit = new SqlDependency(cmdQueryForMedBillsInCase);
                    dependencyMedBillsInCaseEdit.OnChange += new OnChangeEventHandler(OnMedBillsInCaseEditChange);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrMedBillsInCase = cmdQueryForMedBillsInCase.ExecuteReader();

                    gvCasePageMedBills.Rows.Clear();
                    if (rdrMedBillsInCase.HasRows)
                    {
                        while (rdrMedBillsInCase.Read())
                        {
                            DataGridViewRow row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                            if (!rdrMedBillsInCase.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(0) });
                            if (!rdrMedBillsInCase.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(1) });
                            if (!rdrMedBillsInCase.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                            if (!rdrMedBillsInCase.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(3) });
                            if (!rdrMedBillsInCase.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                            if (!rdrMedBillsInCase.IsDBNull(5)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(5) });
                            if (!rdrMedBillsInCase.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(6).ToString("C") });
                            if (!rdrMedBillsInCase.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(7).ToString("C") });
                            if (!rdrMedBillsInCase.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(8).ToString("C") });
                            if (!rdrMedBillsInCase.IsDBNull(9)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(9).ToString("C") });

                            gvCasePageMedBills.Rows.Add(row);
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    btnNewMedBill_Case.Enabled = true;
                    btnEditMedBill.Enabled = true;
                    btnDeleteMedBill.Enabled = true;

                    tbCMMManager.TabPages.Insert(4, tbpgCreateCase);
                    tbCMMManager.SelectedIndex = 4;
                }
            }
            else
            {
                MessageBox.Show("Case screen already open.");
            }
            

        }

        private void ClearMedBillsInCaseEditSafely()
        {
            gvCasePageMedBills.BeginInvoke(new RemoveAllMedBillsInCaseEdit(ClearAllMedBillsInCaseEdit));
        }

        private void AddRowMedBillsInCaseEditSafely(DataGridViewRow row)
        {
            gvCasePageMedBills.BeginInvoke(new AddRowToMedBillsInCaseEdit(AddRowMedBillsInCaseEdit), row);
        }
            

        private void ClearAllMedBillsInCaseEdit()
        {
            gvCasePageMedBills.Rows.Clear();
        }

        private void AddRowMedBillsInCaseEdit(DataGridViewRow row)
        {
            gvCasePageMedBills.Rows.Add(row);
        }

        private void OnMedBillsInCaseEditChange (object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnMedBillsInCaseEditChange;

                UpdateGridViewMedBillsInCaseEdit();

            }
        }

        private void UpdateGridViewMedBillsInCaseEdit()
        {
            String strSqlQueryForMedBillsInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], [dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                                  "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                                  "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                                                  "from ((([dbo].[tbl_medbill] inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId]) " +
                                                  "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id]) " +
                                                  "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id]) " +
                                                  "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                  "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                  "[dbo].[tbl_medbill].[IsDeleted] = 0";

            SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillsInCase, connRN);
            cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

            cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", CaseIdSelected);
            cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", IndividualIdSelected);

            SqlDependency dependencyMedBillsInCaseEdit = new SqlDependency(cmdQueryForMedBillsInCase);
            dependencyMedBillsInCaseEdit.OnChange += new OnChangeEventHandler(OnMedBillsInCaseEditChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrMedBillsInCase = cmdQueryForMedBillsInCase.ExecuteReader();

            //gvCasePageMedBills.Rows.Clear();
            if (IsHandleCreated) ClearMedBillsInCaseEditSafely();
            else gvCasePageMedBills.Rows.Clear();

            if (rdrMedBillsInCase.HasRows)
            {
                while (rdrMedBillsInCase.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    if (!rdrMedBillsInCase.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(0) });
                    if (!rdrMedBillsInCase.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(1) });
                    if (!rdrMedBillsInCase.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                    if (!rdrMedBillsInCase.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(3) });
                    if (!rdrMedBillsInCase.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                    if (!rdrMedBillsInCase.IsDBNull(5)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetString(5) });
                    if (!rdrMedBillsInCase.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(6).ToString("C") });
                    if (!rdrMedBillsInCase.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(7).ToString("C") });
                    if (!rdrMedBillsInCase.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(8).ToString("C") });
                    if (!rdrMedBillsInCase.IsDBNull(9)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillsInCase.GetDecimal(9).ToString("C") });

                    //gvCasePageMedBills.Rows.Add(row);
                    //if (IsHandleCreated) AddRowMedBillsInCaseEdit(row);
                    if (IsHandleCreated) AddRowMedBillsInCaseEditSafely(row);
                    else gvCasePageMedBills.Rows.Add(row);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
        }

        private void btnEditMedBill_Click(object sender, EventArgs e)
        {

            if (!tbCMMManager.TabPages.Contains(tbpgMedicalBill))
            {
                medbillMode = MedBillMode.Edit;

                String MedBillNo = String.Empty;
                int nNumberOfRowSelected = 0;

                for (int i = 0; i < gvCasePageMedBills.Rows.Count; i++)
                {
                    if ((Boolean)gvCasePageMedBills[0, i].Value)
                    {
                        MedBillNo = gvCasePageMedBills[1, i].Value.ToString();
                        nNumberOfRowSelected++;
                    }
                }

                if (nNumberOfRowSelected == 0)
                {
                    MessageBox.Show("Please select a medical bill.");
                    return;
                }
                else if (nNumberOfRowSelected >= 2)
                {
                    MessageBox.Show("More than one medical bill selected.");
                    return;
                }
                else if (nNumberOfRowSelected == 1)
                {
                    //String CaseNameInMedBillEdit = txtCaseName.Text.Trim();
                    //String IndividualIdInMedBillEdit = txtCaseIndividualID.Text.Trim();
                    String IndividualName = String.Empty;

                    String CaseNameInMedBill = txtCaseName.Text.Trim();
                    String IndividualIdInMedBill = txtCaseIndividualID.Text.Trim();
                    //MedBillNo = gvCasePageMedBills[1, i].Value.ToString();


                    //////////////////////////////////////////////////////////////////////////////////

                    //String strCaseNameSelected = String.Empty;
                    String strPatientLastName = txtLastName.Text.Trim();
                    String strPatientFirstName = txtFirstName.Text.Trim();
                    String strPatientMiddleName = txtMiddleName.Text.Trim();
                    String strDateOfBirth = dtpBirthDate.Value.ToString("MM/dd/yyyy");
                    String strSSN = txtIndividualSSN.Text.Trim();
                    String strStreetAddr = txtStreetAddress1.Text.Trim();
                    String strCity = txtCity1.Text.Trim();
                    String strState = txtState1.Text.Trim();
                    String strZip = txtZip1.Text.Trim();

                    txtIndividualIDMedBill.Text = IndividualIdInMedBill;
                    if (strPatientMiddleName.Trim() == String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName;
                    else if (strPatientMiddleName.Trim() != String.Empty) txtPatientNameMedBill.Text = strPatientLastName + ", " + strPatientFirstName + " " + strPatientMiddleName;

                    txtMedBillDOB.Text = strDateOfBirth;
                    txtMedBillSSN.Text = strSSN;
                    txtMedBillAddress.Text = strStreetAddr + ", " + strCity + ", " + strState + " " + strZip;

                    ///////////////////////////////////////////////////////////////////////////////////
                    // MedBillNo, CaseNameInMedBill, IndividualIdInMedBill
                    String strSqlQueryForMedBillTypes = "select [dbo].[tbl_medbill_type].[MedBillTypeId], [dbo].[tbl_medbill_type].[MedBillTypeName] from [dbo].[tbl_medbill_type]";

                    SqlCommand cmdQueryForMedBillTypes = new SqlCommand(strSqlQueryForMedBillTypes, connRN);
                    cmdQueryForMedBillTypes.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrMedBillTypes = cmdQueryForMedBillTypes.ExecuteReader();
                    dicMedBillTypes.Clear();

                    if (rdrMedBillTypes.HasRows)
                    {
                        while(rdrMedBillTypes.Read())
                        {
                            if (!rdrMedBillTypes.IsDBNull(0) && !rdrMedBillTypes.IsDBNull(1))
                            {
                                dicMedBillTypes.Add(rdrMedBillTypes.GetInt16(0), rdrMedBillTypes.GetString(1));
                            }
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();
                    /////////////////////////////////////////////////////////////////////////////////////////
                    ///

                    // Populate Pending Reason
                    comboPendingReason.Items.Clear();
                    if (dicPendingReason.Count > 0)
                    {
                        for (int i = 0; i < dicPendingReason.Count; i++)
                        {
                            comboPendingReason.Items.Add(dicPendingReason[i]);
                        }
                        comboPendingReason.SelectedIndex = 0;
                    }

                    // Populate Ineligible Reason
                    comboIneligibleReason.Items.Clear();
                    if (dicIneligibleReason.Count > 0)
                    {
                        for (int i = 0; i < dicIneligibleReason.Count; i++)
                        {
                            comboIneligibleReason.Items.Add(dicIneligibleReason[i]);
                        }
                        comboIneligibleReason.SelectedIndex = 0;
                    }

                    // Get the Medical Bill Note Type info
                    //List<MedBillNoteTypeInfo> lstMedBillNoteTypeInfo = new List<MedBillNoteTypeInfo>();

                    //String strSqlQueryForMedBillNoteTypeInfo = "select [dbo].[tbl_MedBillNoteType].[MedBillNoteTypeId], [dbo].[tbl_MedBillNoteType].[MedBillNoteTypeValue] from [dbo].[tbl_MedBillNoteType]";

                    //SqlCommand cmdQueryForMedBillNoteTypeInfo = new SqlCommand(strSqlQueryForMedBillNoteTypeInfo, connRN);
                    //cmdQueryForMedBillNoteTypeInfo.CommandType = CommandType.Text;

                    //connRN.Open();
                    //SqlDataReader rdrMedBillNoteType = cmdQueryForMedBillNoteTypeInfo.ExecuteReader();
                    //if (rdrMedBillNoteType.HasRows)
                    //{
                    //    while (rdrMedBillNoteType.Read())
                    //    {
                    //        if (!rdrMedBillNoteType.IsDBNull(0) && !rdrMedBillNoteType.IsDBNull(1))
                    //        {
                    //            lstMedBillNoteTypeInfo.Add(new MedBillNoteTypeInfo { MedBillNoteTypeId = rdrMedBillNoteType.GetInt16(0), MedBillNoteTypeValue = rdrMedBillNoteType.GetString(1) });
                    //        }
                    //    }
                    //}
                    //connRN.Close();

                    // Get medical bill info
                    String ICD10Code = String.Empty;
                    String strSqlQueryForMedBillEdit = "select [dbo].[tbl_medbill].[Case_Id], [dbo].[tbl_medbill].[Illness_Id], [dbo].[tbl_medbill].[Incident_Id], " +
                                                       "[dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill].[MedBillType_Id], [dbo].[tbl_medbill].[BillStatus], " +
                                                       "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_MedicalProvider].[Name], " +
                                                       "[dbo].[tbl_medbill].[PrescriptionDrugName], [dbo].[tbl_medbill].[PrescriptionNo], [dbo].[tbl_medbill].[PrescriptionDescription], " +
                                                       "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy], [dbo].[tbl_medbill].[PatientTypeId], " +
                                                       "[dbo].[tbl_medbill].[BillDate], [dbo].[tbl_medbill].[DueDate], " +
                                                       "[dbo].[tbl_medbill].[Account_At_Provider], [dbo].[tbl_MedicalProvider].[PHONE], [dbo].[tbl_medbill].[ProviderContactPerson], " +
                                                       "[dbo].[tbl_medbill].[Note], " +
                                                       "[dbo].[tbl_illness].[ICD_10_Id], " +
                                                       "[dbo].[tbl_medbill].[PendingReason], [dbo].[tbl_medbill].[IneligibleReason], " +
                                                       "[dbo].[tbl_medbill].[Account_At_Provider], [dbo].[tbl_medbill].[ProviderPhoneNumber], [dbo].[tbl_medbill].[ProviderContactPerson], " +
                                                       "[dbo].[tbl_medbill].[ProposalLetterSentDate], [dbo].[tbl_medbill].[HIPPASentDate], [dbo].[tbl_medbill].[MedicalRecordDate] " +
                                                       "from (([dbo].[tbl_medbill] inner join [dbo].[tbl_illness] on [dbo].[tbl_medbill].[Illness_Id] = [dbo].[tbl_illness].[Illness_Id]) " +
                                                       "inner join [dbo].[tbl_MedicalProvider] on [dbo].[tbl_medbill].[MedicalProvider_Id] = [dbo].[tbl_MedicalProvider].[ID]) " +
                                                       "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and " +
                                                       "[dbo].[tbl_medbill].[Case_Id] = @CaseName and " +
                                                       "[dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                                       "[dbo].[tbl_medbill].[IsDeleted] = 0";

                    SqlCommand cmdQueryForMedBillEdit = new SqlCommand(strSqlQueryForMedBillEdit, connRN);
                    cmdQueryForMedBillEdit.CommandType = CommandType.Text;

                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@CaseName", CaseNameInMedBill);
                    cmdQueryForMedBillEdit.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrMedBillEdit = cmdQueryForMedBillEdit.ExecuteReader();
                    if (rdrMedBillEdit.HasRows)
                    {
                        rdrMedBillEdit.Read();

                        if (!rdrMedBillEdit.IsDBNull(0)) txtMedBill_CaseNo.Text = rdrMedBillEdit.GetString(0).Trim();
                        if (!rdrMedBillEdit.IsDBNull(1)) Illness.IllnessId = rdrMedBillEdit.GetString(1).Trim();
                        if (!rdrMedBillEdit.IsDBNull(2)) txtMedBill_Incident.Text = rdrMedBillEdit.GetString(2).Trim();

                        if (!rdrMedBillEdit.IsDBNull(3)) txtMedBillNo.Text = rdrMedBillEdit.GetString(3).Trim();
                        if (!rdrMedBillEdit.IsDBNull(4))
                        {
                            // populate medical bill types
                            comboMedBillType.Items.Clear();
                            for (int i = 1; i <= dicMedBillTypes.Count; i++)
                            {
                                comboMedBillType.Items.Add(dicMedBillTypes[i]);
                            }
                            comboMedBillType.SelectedIndex = rdrMedBillEdit.GetInt16(4) - 1;
                        }

                        if (!rdrMedBillEdit.IsDBNull(5))
                        {
                            comboMedBillStatus.Items.Clear();
                            if (dicMedBillStatus.Count > 0)
                            {
                                for (int i = 0; i < dicMedBillStatus.Count; i++)
                                {
                                    comboMedBillStatus.Items.Add(dicMedBillStatus[i]);
                                }
                                //comboMedBillStatus.SelectedIndex = 0;
                                comboMedBillStatus.SelectedIndex = rdrMedBillEdit.GetInt16(5);
                            }
                        }
                        if (!rdrMedBillEdit.IsDBNull(6))
                        {
                            txtMedBillAmount.Text = rdrMedBillEdit.GetDecimal(6).ToString("C");
                            txtBalance.Text = rdrMedBillEdit.GetDecimal(6).ToString("C");
                        }
                        if (!rdrMedBillEdit.IsDBNull(7)) txtMedicalProvider.Text = rdrMedBillEdit.GetString(7).Trim();
                        if (!rdrMedBillEdit.IsDBNull(8)) txtPrescriptionName.Text = rdrMedBillEdit.GetString(8).Trim();
                        if (!rdrMedBillEdit.IsDBNull(9)) txtNumberOfMedication.Text = rdrMedBillEdit.GetString(9).Trim();
                        if (!rdrMedBillEdit.IsDBNull(10)) txtPrescriptionDescription.Text = rdrMedBillEdit.GetString(10).Trim();
                        if (!rdrMedBillEdit.IsDBNull(11)) txtNumPhysicalTherapy.Text = rdrMedBillEdit.GetInt16(11).ToString();
                        if (!rdrMedBillEdit.IsDBNull(12))
                        {
                            int nPatientType = rdrMedBillEdit.GetInt16(12);

                            if (nPatientType == 0) rbOutpatient.Checked = true;
                            else if (nPatientType == 1) rbInpatient.Checked = true;
                        }
                        // Bill date
                        if (!rdrMedBillEdit.IsDBNull(13))
                        {
                            dtpBillDate.Text = rdrMedBillEdit.GetDateTime(13).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            dtpBillDate.Format = DateTimePickerFormat.Custom;
                            dtpBillDate.CustomFormat = " ";
                        }

                        // Due date
                        if (!rdrMedBillEdit.IsDBNull(14))
                        {
                            dtpDueDate.Text = rdrMedBillEdit.GetDateTime(14).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            dtpDueDate.Format = DateTimePickerFormat.Custom;
                            dtpDueDate.CustomFormat = " ";
                        }

                        if (!rdrMedBillEdit.IsDBNull(15)) txtMedBillAccountNoAtProvider.Text = rdrMedBillEdit.GetString(15);
                        if (!rdrMedBillEdit.IsDBNull(16)) txtMedProviderPhoneNo.Text = rdrMedBillEdit.GetString(16);
                        if (!rdrMedBillEdit.IsDBNull(17)) txtProviderContactPerson.Text = rdrMedBillEdit.GetString(17);

                        if (!rdrMedBillEdit.IsDBNull(18))
                        {
                            if (comboMedBillType.SelectedItem.ToString() == "Medical Bill") txtMedBillNote.Text = rdrMedBillEdit.GetString(18);
                            if (comboMedBillType.SelectedItem.ToString() == "Prescription") txtPrescriptionNote.Text = rdrMedBillEdit.GetString(18);
                            if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy") txtPhysicalTherapyRxNote.Text = rdrMedBillEdit.GetString(18);
                        }

                        if (!rdrMedBillEdit.IsDBNull(19))
                        {
                            ICD10Code = rdrMedBillEdit.GetString(19).Trim();
                            Illness.ICD10Code = ICD10Code;
                            txtMedBill_Illness.Text = ICD10Code;
                        }

                        if ((comboMedBillType.SelectedIndex == 0)&&(!rdrMedBillEdit.IsDBNull(20)))
                        {
                            comboPendingReason.SelectedIndex = rdrMedBillEdit.GetInt32(20);
                        }

                        if ((comboMedBillType.SelectedIndex == 0)&&(!rdrMedBillEdit.IsDBNull(21)))
                        {
                            comboIneligibleReason.SelectedIndex = rdrMedBillEdit.GetInt32(21);
                        }

                        //comboMedBillType.SelectedIndex = rdrMedBillEdit.GetInt16(4) - 1;

                        if (comboMedBillType.SelectedIndex == 0)       // Medical Bill Type - Medical Bill
                        {
                            txtPrescriptionName.Text = String.Empty;
                            txtPrescriptionDescription.Text = String.Empty;
                            txtPrescriptionNote.Text = String.Empty;
                            txtNumberOfMedication.Text = String.Empty;

                            txtNumPhysicalTherapy.Text = String.Empty;
                            txtPhysicalTherapyRxNote.Text = String.Empty;
                        }
                        else if (comboMedBillType.SelectedIndex == 1)       // Medical Bill Type - Prescription
                        {
                            txtNumPhysicalTherapy.Text = String.Empty;
                            txtPhysicalTherapyRxNote.Text = String.Empty;

                            rbInpatient.Checked = false;
                            rbOutpatient.Checked = false;

                            comboPendingReason.SelectedIndex = 0;
                            comboIneligibleReason.SelectedIndex = 0;

                            txtMedBillNote.Text = String.Empty;
                        }
                        else if (comboMedBillType.SelectedIndex == 2)       // Medical Bill Type - Physical Therapy
                        {
                            txtPrescriptionName.Text = String.Empty;
                            txtPrescriptionDescription.Text = String.Empty;
                            txtPrescriptionNote.Text = String.Empty;
                            txtNumberOfMedication.Text = String.Empty;

                            rbInpatient.Checked = false;
                            rbOutpatient.Checked = false;

                            comboPendingReason.SelectedIndex = 0;
                            comboIneligibleReason.SelectedIndex = 0;

                            txtMedBillNote.Text = String.Empty;
                        }

                        if (!rdrMedBillEdit.IsDBNull(22)) txtMedBillAccountNoAtProvider.Text = rdrMedBillEdit.GetString(22);
                        if (!rdrMedBillEdit.IsDBNull(23)) txtMedProviderPhoneNo.Text = rdrMedBillEdit.GetString(23);
                        if (!rdrMedBillEdit.IsDBNull(24)) txtProviderContactPerson.Text = rdrMedBillEdit.GetString(24);

                        if (!rdrMedBillEdit.IsDBNull(25))
                        {
                            dtpProposalLetterSentDate.Value = rdrMedBillEdit.GetDateTime(25);
                            dtpProposalLetterSentDate.Format = DateTimePickerFormat.Short;
                        }
                        if (!rdrMedBillEdit.IsDBNull(26))
                        {
                            dtpHippaSentDate.Value = rdrMedBillEdit.GetDateTime(26);
                            dtpHippaSentDate.Format = DateTimePickerFormat.Short;
                        }
                        if (!rdrMedBillEdit.IsDBNull(27))
                        {
                            dtpMedicalRecordDate.Value = rdrMedBillEdit.GetDateTime(27);
                            dtpMedicalRecordDate.Format = DateTimePickerFormat.Short;
                        }
                        
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    // Calculate the personal responsibility balance
                    //String IncidentNo = txtMedBill_Incident.Text.Trim();
                    //String IndividualId = txtCaseIndividualID.Text.Trim();

                    //String strSqlQueryForIncidentChange = "select [cdc].[dbo_tbl_incident_CT].[Program_id], [dbo].[tbl_program].[ProgramName] from [cdc].[dbo_tbl_incident_CT] " +
                    //                                      "inner join [dbo].[tbl_program] on [cdc].[dbo_tbl_incident_CT].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                    //                                      "where [cdc].[dbo_tbl_incident_CT].[Incident_id] = @IncidentId and [cdc].[dbo_tbl_incident_CT].[Individual_id] = @IndividualId and " +
                    //                                      "([cdc].[dbo_tbl_incident_CT].[__$operation] = 2 or [cdc].[dbo_tbl_incident_CT].[__$operation] = 3 or " +
                    //                                      "[cdc].[dbo_tbl_incident_CT].[__$operation] = 4) " +      // capture incident program for insert, update
                    //                                      "order by [cdc].[dbo_tbl_incident_CT].[Program_id]";

                    //SqlCommand cmdQueryForIncidentChange = new SqlCommand(strSqlQueryForIncidentChange, connRN);
                    //cmdQueryForIncidentChange.CommandType = CommandType.Text;

                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IncidentId", IncidentNo);
                    //cmdQueryForIncidentChange.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //connRN.Open();
                    //SqlDataReader rdrIncidentChange = cmdQueryForIncidentChange.ExecuteReader();
                    //if (rdrIncidentChange.HasRows)
                    //{
                    //    while (rdrIncidentChange.Read())
                    //    {
                    //        //lstIncidentProgramInfo.Add(new IncidentProgramInfo { IncidentProgramId = rdrIncidentChange.GetInt16(0), IncidentProgramName = rdrIncidentChange.GetString(1).Trim() });
                    //        IncidentProgramInfo incidentProgram = new IncidentProgramInfo(rdrIncidentChange.GetInt16(0), rdrIncidentChange.GetString(1).Trim());
                    //        lstIncidentProgramInfo.Add(incidentProgram);
                    //    }
                    //}
                    //connRN.Close();

                    //Boolean bBronze = false;
                    //Boolean bSilver = false;
                    //Boolean bGold = false;
                    //Boolean bGoldPlus = false;
                    //Boolean bGoldMed1 = false;
                    //Boolean bGoldMed2 = false;

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.IncidentProgramId == 3)
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bBronze = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 2) && (bBronze == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        bSilver = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 1) && (bBronze == false) && (bSilver == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 0) && (bBronze == false) && (bSilver == false) && (bGold == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 4) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if ((incidentInfo.IncidentProgramId == 5) && (bBronze == false) && (bSilver == false) && (bGold == false) && (bGoldPlus == false) && (bGoldMed1 == false))
                    //    {
                    //        incidentInfo.bPersonalResponsibilityProgram = true;
                    //        break;
                    //    }
                    //}

                    //foreach (IncidentProgramInfo incidentInfo in lstIncidentProgramInfo)
                    //{
                    //    if (incidentInfo.bPersonalResponsibilityProgram == true)
                    //        PersonalResponsibilityAmountInMedBill = incidentInfo.PersonalResponsibilityAmount;
                    //}

                    // Get disease name
                    String strSqlQueryForDiseaseName = "select [dbo].[ICD10 Code].[Name] from [dbo].[ICD10 Code] where [dbo].[ICD10 Code].[ICD10_CODE__C] = @ICD10Code";

                    SqlCommand cmdQueryForDiseaseName = new SqlCommand(strSqlQueryForDiseaseName, connSalesforce);
                    cmdQueryForDiseaseName.CommandType = CommandType.Text;

                    cmdQueryForDiseaseName.Parameters.AddWithValue("@ICD10Code", ICD10Code);

                    if (connSalesforce.State == ConnectionState.Open)
                    {
                        connSalesforce.Close();
                        connSalesforce.Open();
                    }
                    else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();
                    //String DiseaseName = cmdQueryForDiseaseName.ExecuteScalar().ToString();
                    Object objDiseaseName = cmdQueryForDiseaseName.ExecuteScalar();
                    if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                    String DiseaseName = String.Empty;

                    if (objDiseaseName != null) DiseaseName = objDiseaseName.ToString();
                    else
                    {
                        MessageBox.Show("No Disease Name for the ICD 10 Code: " + ICD10Code, "Error", MessageBoxButtons.OK);
                        return;
                    }

                    txtMedBill_ICD10Code.Text = ICD10Code;
                    txtMedBillDiseaseName.Text = DiseaseName;

                    // Get documents info
                    String strSqlQueryForDocumentsInfo = "select [dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                         "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
                                                         "from [dbo].[tbl_case] where [dbo].[tbl_case].[Case_Name] = @CaseId and " +
                                                         "[dbo].[tbl_case].[Contact_ID] = @IndividualId and" +
                                                         "[dbo].[tbl_case].[IsDeleted] = 0";

                    SqlCommand cmdQueryForDocInfo = new SqlCommand(strSqlQueryForDocumentsInfo, connRN);
                    cmdQueryForDocInfo.CommandType = CommandType.Text;

                    cmdQueryForDocInfo.Parameters.AddWithValue("@CaseId", CaseNameInMedBill);
                    cmdQueryForDocInfo.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrDocInfo = cmdQueryForDocInfo.ExecuteReader();
                    if (rdrDocInfo.HasRows)
                    {
                        rdrDocInfo.Read();

                        if (!rdrDocInfo.IsDBNull(0))
                        {
                            if (rdrDocInfo.GetBoolean(0))
                            {
                                chkMedBillNPFReceived.Checked = true;
                                chkMedBillNPFReceived.Enabled = false;
                                dtpMedBillNPF.Text = rdrDocInfo.GetDateTime(1).ToString("MM/dd/yyyy");
                                dtpMedBillNPF.Enabled = false;
                                btnViewNPF.Enabled = true;
                            }
                            else
                            {
                                chkMedBillNPFReceived.Checked = false;
                                chkMedBillNPFReceived.Enabled = false;
                                dtpMedBillNPF.Format = DateTimePickerFormat.Custom;
                                dtpMedBillNPF.CustomFormat = " ";
                                dtpMedBillNPF.Enabled = false;
                                btnViewNPF.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(2))
                        {
                            if (rdrDocInfo.GetBoolean(2))
                            {
                                chkMedBill_IBReceived.Checked = true;
                                chkMedBill_IBReceived.Enabled = false;
                                dtpMedBill_IB.Text = rdrDocInfo.GetDateTime(3).ToString("MM/dd/yyyy");
                                dtpMedBill_IB.Enabled = false;
                                btnViewIB.Enabled = true;
                            }
                            else
                            {
                                chkMedBill_IBReceived.Checked = false;
                                chkMedBill_IBReceived.Enabled = false;
                                dtpMedBill_IB.Format = DateTimePickerFormat.Custom;
                                dtpMedBill_IB.CustomFormat = " ";
                                dtpMedBill_IB.Enabled = false;
                                btnViewIB.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(4))
                        {
                            if (rdrDocInfo.GetBoolean(4))
                            {
                                chkMedBillPOPReceived.Checked = true;
                                chkMedBillPOPReceived.Enabled = false;
                                dtpMedBillPOP.Text = rdrDocInfo.GetDateTime(5).ToString("MM/dd/yyyy");
                                dtpMedBillPOP.Enabled = false;
                                btnViewPoP.Enabled = true;
                            }
                            else
                            {
                                chkMedBillPOPReceived.Checked = false;
                                chkMedBillPOPReceived.Enabled = false;
                                dtpMedBillPOP.Format = DateTimePickerFormat.Custom;
                                dtpMedBillPOP.CustomFormat = " ";
                                dtpMedBillPOP.Enabled = false;
                                btnViewPoP.Enabled = false;
                            }
                        }
                        if (!rdrDocInfo.IsDBNull(6))
                        {
                            if (rdrDocInfo.GetBoolean(6))
                            {
                                chkMedRecordReceived.Checked = true;
                                chkMedRecordReceived.Enabled = false;
                                dtpMedBillMedRecord.Text = rdrDocInfo.GetDateTime(7).ToString("MM/dd/yyyy");
                                dtpMedBillMedRecord.Enabled = false;
                                btnViewMedRecord.Enabled = true;
                            }
                            else
                            {
                                chkMedRecordReceived.Checked = false;
                                chkMedRecordReceived.Enabled = false;
                                dtpMedBillMedRecord.Format = DateTimePickerFormat.Custom;
                                dtpMedBillMedRecord.CustomFormat = " ";
                                dtpMedBillMedRecord.Enabled = false;
                                btnViewMedRecord.Enabled = false;
                            }
                        }

                        if (!rdrDocInfo.IsDBNull(8))
                        {
                            if (rdrDocInfo.GetBoolean(8))
                            {
                                chkOtherDocReceived.Checked = true;
                                chkOtherDocReceived.Enabled = false;
                                dtpMedBillOtherDoc.Text = rdrDocInfo.GetDateTime(9).ToString("MM/dd/yyyy");
                                dtpMedBillOtherDoc.Enabled = false;
                                btnViewOtherDoc.Enabled = true;
                            }
                            else
                            {
                                chkOtherDocReceived.Checked = false;
                                chkOtherDocReceived.Enabled = false;
                                dtpMedBillOtherDoc.Format = DateTimePickerFormat.Custom;
                                dtpMedBillOtherDoc.CustomFormat = " ";
                                dtpMedBillOtherDoc.Enabled = false;
                                btnViewOtherDoc.Enabled = false;
                            }
                        }

                        strCaseIdSelected = CaseNameInMedBill;
                        strContactIdSelected = IndividualIdInMedBill;
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    //String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] where [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                    //SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                    //cmdQueryForIllnessId.CommandType = CommandType.Text;

                    //cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", Illness.ICD10Code);

                    //connRN.Open();
                    //Illness.IllnessId = cmdQueryForIllnessId.ExecuteScalar().ToString();
                    //connRN.Close();


                    String strSqlQueryForIncidentProgram = "select [dbo].[tbl_program].[ProgramName] from [dbo].[tbl_program] inner join [dbo].[tbl_incident] " +
                                                           "on [dbo].[tbl_program].[Program_id] = [dbo].[tbl_incident].[Program_id] " +
                                                           "where [dbo].[tbl_incident].[Individual_id] = @IndividualId and [dbo].[tbl_incident].[Incident_id] = @IncidentId";

                    SqlCommand cmdQueryForIncidentProgram = new SqlCommand(strSqlQueryForIncidentProgram, connRN);
                    cmdQueryForIncidentProgram.CommandType = CommandType.Text;

                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IndividualId", IndividualIdInMedBill);
                    cmdQueryForIncidentProgram.Parameters.AddWithValue("@IncidentId", txtMedBill_Incident.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String IncidentProgramName = cmdQueryForIncidentProgram.ExecuteScalar().ToString();
                    Object objIncidentProgramName = cmdQueryForIncidentProgram.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String IncidentProgramName = String.Empty;
                    if (objIncidentProgramName != null) IncidentProgramName = objIncidentProgramName.ToString();
                    else
                    {
                        MessageBox.Show("No Program Name for the Incident Id: " + txtMedBill_Incident.Text.Trim(), "Error", MessageBoxButtons.OK);
                        return;
                    }

                    if (IncidentProgramName != null) txtIncdProgram.Text = IncidentProgramName;

                    if (txtIncdProgram.Text.Trim() != txtMemberProgram.Text.Trim())
                    {
                        txtIncdProgram.BackColor = Color.Red;
                        txtMemberProgram.BackColor = Color.Red;
                    }
                    else if (txtIncdProgram.Text.Trim() == txtMemberProgram.Text.Trim())
                    {
                        txtIncdProgram.BackColor = Color.White;
                        txtMemberProgram.BackColor = Color.FromKnownColor(KnownColor.Control);
                    }


                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    String strSqlQueryForMedicalProvider = "select dbo.tbl_MedicalProvider.ID, dbo.tbl_MedicalProvider.Name, dbo.tbl_MedicalProvider.Type from dbo.tbl_MedicalProvider";

                    SqlCommand cmdQueryForMedicalProvider = new SqlCommand(strSqlQueryForMedicalProvider, connRN);
                    cmdQueryForMedicalProvider.CommandType = CommandType.Text;

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();

                    SqlDataReader rdrMedicalProvider = cmdQueryForMedicalProvider.ExecuteReader();

                    lstMedicalProvider.Clear();
                    if (rdrMedicalProvider.HasRows)
                    {
                        while (rdrMedicalProvider.Read())
                        {
                            MedicalProviderInfo info = new MedicalProviderInfo();

                            if (!rdrMedicalProvider.IsDBNull(0)) info.ID = rdrMedicalProvider.GetString(0);
                            if (!rdrMedicalProvider.IsDBNull(1)) info.Name = rdrMedicalProvider.GetString(1);
                            if (!rdrMedicalProvider.IsDBNull(2)) info.Type = rdrMedicalProvider.GetString(2);

                            lstMedicalProvider.Add(info);
                        }
                    }

                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    var srcMedicalProvider = new AutoCompleteStringCollection();

                    for (int i = 0; i < lstMedicalProvider.Count; i++)
                    {
                        srcMedicalProvider.Add(lstMedicalProvider[i].Name);
                    }

                    txtMedicalProvider.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtMedicalProvider.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtMedicalProvider.AutoCompleteCustomSource = srcMedicalProvider;


                    /// Put code here

                    String strSqlQueryForSettlement = "select [dbo].[tbl_settlement].[Name], [dbo].[tbl_settlement_type_code].[SettlementTypeValue], [dbo].[tbl_settlement].[Amount], " +
                                  "[dbo].[tbl_settlement].[PersonalResponsibilityCredit], [dbo].[tbl_payment_method].[PaymentMethod_Value], " +
                                  "[dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate], " +
                                  "[dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
                                  "[dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], [dbo].[tbl_settlement].[ACH_Reconciled], " +
                                  "[dbo].[tbl_Credit_Card__c].[Name], [dbo].[tbl_settlement].[CMMCreditCardPaidDate], [dbo].[tbl_settlement].[CC_Reconciled], " +
                                  "[dbo].[tbl_settlement].[AllowedAmount], [dbo].[tbl_settlement].[IneligibleReason], " +
                                  "[dbo].[tbl_settlement].[Notes] " +
                                  "from [dbo].[tbl_settlement] inner join [dbo].[tbl_settlement_type_code] " +
                                  "on [dbo].[tbl_settlement].[SettlementType] = [dbo].[tbl_settlement_type_code].[SettlementTypeCode] " +
                                  "inner join [dbo].[tbl_payment_method] on [dbo].[tbl_settlement].[CMMPaymentMethod] = [dbo].[tbl_payment_method].[PaymentMethod_Id] " +
                                  "inner join [dbo].[tbl_Credit_Card__c] on [dbo].[tbl_settlement].[CMMCreditCard] = [dbo].[tbl_Credit_Card__c].[CreditCard_Id]" +
                                  "where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillNo and " +
                                  "[dbo].[tbl_settlement].[IsDeleted] = 0 " +
                                  "order by [dbo].[tbl_settlement].[Name]";

                    SqlCommand cmdQueryForSettlement = new SqlCommand(strSqlQueryForSettlement, connRN);
                    cmdQueryForSettlement.CommandType = CommandType.Text;

                    cmdQueryForSettlement.Parameters.AddWithValue("@MedBillNo", MedBillNo);

                    SqlDependency dependencySettlementInMedBill = new SqlDependency(cmdQueryForSettlement);
                    dependencySettlementInMedBill.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillEditChange);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    SqlDataReader rdrSettlement = cmdQueryForSettlement.ExecuteReader();
                    gvSettlementsInMedBill.Rows.Clear();

                    if (rdrSettlement.HasRows)
                    {
                        while (rdrSettlement.Read())
                        {
                            DataGridViewRow row = new DataGridViewRow();
                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                            if (!rdrSettlement.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(0) });
                            if (!rdrSettlement.IsDBNull(1))
                            {
                                DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstSettlementType.Count; i++)
                                {
                                    comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                                }
                                for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                        comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                                }

                                row.Cells.Add(comboCellSettlementType);
                            }
                            else
                            {
                                DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstSettlementType.Count; i++)
                                {
                                    comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                                }

                                for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                        comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                                }

                                row.Cells.Add(comboCellSettlementType);
                            }

                            if (!rdrSettlement.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(2).ToString("C") });
                            else
                            {
                                Decimal Zero = 0;
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                            }


                            if (!rdrSettlement.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(3).ToString("C") });
                            else
                            {
                                Decimal Zero = 0;
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                            }

                            /////////////////////////////////////////////////////////////////////
                            if (!rdrSettlement.IsDBNull(4))
                            {
                                DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstPaymentMethod.Count; i++)
                                {
                                    if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                    else comboCellPaymentMethod.Items.Add(String.Empty);
                                }

                                for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
                                {
                                    if (rdrSettlement.GetString(4) == comboCellPaymentMethod.Items[i].ToString())
                                        comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
                                }

                                row.Cells.Add(comboCellPaymentMethod);
                            }
                            else
                            {
                                DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                                for (int i = 0; i < lstPaymentMethod.Count; i++)
                                {
                                    if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                    else comboCellPaymentMethod.Items.Add(String.Empty);
                                    //comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                                }

                                for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
                                {
                                    if ((!rdrSettlement.IsDBNull(4)) && comboCellPaymentMethod.Items[i] != null)
                                    {
                                        if (rdrSettlement.GetString(4) == comboCellPaymentMethod.Items[i].ToString())
                                            comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
                                    }
                                    else comboCellPaymentMethod.Value = null;
                                }

                                row.Cells.Add(comboCellPaymentMethod);

                            }

                            /////////////////////////////////////////////////////////////////////
                            if (!rdrSettlement.IsDBNull(5))
                            {

                                DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                                approvedCell.Value = rdrSettlement.GetBoolean(5);
                                approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                row.Cells.Add(approvedCell);
                            }
                            else
                            {
                                DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                                approvedCell.Value = false;
                                approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                row.Cells.Add(approvedCell);
                            }

                            if (!rdrSettlement.IsDBNull(6)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(6).ToString("MM/dd/yyyy")) });
                            else row.Cells.Add(new CalendarCell { Value = null });

                            // Payment information
                            if (!rdrSettlement.IsDBNull(4))
                            {
                                String strPaymentMethod = rdrSettlement.GetString(4);

                                switch (strPaymentMethod)
                                {
                                    case "Check":
                                        if (!rdrSettlement.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(7) });
                                        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNoneForCheck = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCardNoneForCheck.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNoneForCheck.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNoneForCheck);
                                        if (!rdrSettlement.IsDBNull(8)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(8).ToString("MM/dd/yyyy")) });
                                        if (!rdrSettlement.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(9) });
                                        break;
                                    case "ACH/Banking":
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        if (!rdrSettlement.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(10) });
                                        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNoneForACH = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null)
                                                comboCellCreditCardNoneForACH.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNoneForACH.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNoneForACH);
                                        if (!rdrSettlement.IsDBNull(11)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(11).ToString("MM/dd/yyyy")) });
                                        if (!rdrSettlement.IsDBNull(12)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(12) });
                                        break;
                                    case "Credit Card":
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        if (!rdrSettlement.IsDBNull(13))
                                        {
                                            DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                            for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                            {
                                                comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            }
                                            for (int i = 0; i < comboCellCreditCard.Items.Count; i++)
                                            {
                                                if (rdrSettlement.GetString(13) == comboCellCreditCard.Items[i].ToString())
                                                    comboCellCreditCard.Value = comboCellCreditCard.Items[i];
                                            }
                                            row.Cells.Add(comboCellCreditCard);
                                        }
                                        else
                                        {
                                            DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                            for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                            {
                                                comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            }
                                            row.Cells.Add(comboCellCreditCard);
                                        }
                                        if (!rdrSettlement.IsDBNull(14)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(14).ToString("MM/dd/yyyy")) });
                                        if (!rdrSettlement.IsDBNull(15)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(15) });
                                        break;
                                    default:
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                        DataGridViewComboBoxCell comboCellCreditCardNone = new DataGridViewComboBoxCell();
                                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                        {
                                            if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCardNone.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                            else comboCellCreditCardNone.Items.Add(String.Empty);
                                        }
                                        row.Cells.Add(comboCellCreditCardNone);
                                        row.Cells.Add(new CalendarCell { Value = null });
                                        row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });
                                        break;
                                }
                            }
                            else
                            {

                                DataGridViewTextBoxCell txtCheckNoCell = new DataGridViewTextBoxCell();
                                txtCheckNoCell.Value = null;
                                row.Cells.Add(txtCheckNoCell);
                                DataGridViewTextBoxCell txtACHNoCell = new DataGridViewTextBoxCell();
                                txtACHNoCell.Value = null;
                                row.Cells.Add(txtACHNoCell);
                                DataGridViewComboBoxCell comboCreditCardCell = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                {
                                    if (lstCreditCardInfo[i].CreditCardNo != null) comboCreditCardCell.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                    else comboCreditCardCell.Items.Add(String.Empty);
                                }
                                row.Cells.Add(comboCreditCardCell);
                                comboCreditCardCell.ReadOnly = true;
                                CalendarCell calPaymentDate = new CalendarCell();
                                calPaymentDate.Value = null;
                                row.Cells.Add(calPaymentDate);
                                DataGridViewCheckBoxCell chkReconciledCell = new DataGridViewCheckBoxCell();
                                chkReconciledCell.Value = false;
                                row.Cells.Add(chkReconciledCell);

                            }


                            if (!rdrSettlement.IsDBNull(16)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(16).ToString("C") });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                            if (!rdrSettlement.IsDBNull(17))
                            {
                                if (dicIneligibleReason.Count > 0)
                                {
                                    DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < dicIneligibleReason.Count; i++)
                                    {
                                        comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                                    }
                                    comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[rdrSettlement.GetInt32(17)];
                                    row.Cells.Add(comboCellIneligibleReason);
                                }
                            }
                            else
                            {
                                if (dicIneligibleReason.Count > 0)
                                {
                                    DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < dicIneligibleReason.Count; i++)
                                    {
                                        comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                                    }
                                    comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[0];
                                    row.Cells.Add(comboCellIneligibleReason);
                                }
                            }

                            if (!rdrSettlement.IsDBNull(18)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(18) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                            gvSettlementsInMedBill.Rows.Add(row);
                        }
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (txtIncdProgram.Text.Trim() != String.Empty)
                    {

                        String IncidentProgram = txtIncdProgram.Text.Trim();
                        Decimal PersonalResponsibilityAmount = 0;

                        String strSqlQueryForPersonalResponsibilityAmount = "select [dbo].[tbl_program].[PersonalResponsibilityAmount] from [dbo].[tbl_program] " +
                                                                            "where [dbo].[tbl_program].[ProgramName] = @ProgramName";

                        SqlCommand cmdQueryForPersonalResponsibilityAmount = new SqlCommand(strSqlQueryForPersonalResponsibilityAmount, connRN);
                        cmdQueryForPersonalResponsibilityAmount.CommandType = CommandType.Text;

                        cmdQueryForPersonalResponsibilityAmount.Parameters.AddWithValue("@ProgramName", IncidentProgram);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        SqlDataReader rdrPersonalResponsibilityAmount = cmdQueryForPersonalResponsibilityAmount.ExecuteReader();
                        if (rdrPersonalResponsibilityAmount.HasRows)
                        {

                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();
                    }

                    //foreach (IncidentProgramInfo incdProgram in lstIncidentProgramInfo)
                    //{
                    //    if (incdProgram.bPersonalResponsibilityProgram == true) PersonalResponsibilityAmount = incdProgram.PersonalResponsibilityAmount;
                    //}

                    //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    //{
                    //    if (gvSettlementsInMedBill["PersonalResponsibility", i]?.Value != null)
                    //    {
                    //        Decimal result = 0;
                    //        if (Decimal.TryParse(gvSettlementsInMedBill["PersonalResponsibility", i]?.Value?.ToString(), NumberStyles.Currency, new CultureInfo("en-US"), out result))
                    //        {
                    //            PersonalResponsibilityAmount -= result;
                    //        }
                    //    }
                    //}

                    //txtPersonalResponsibility.Text = PersonalResponsibilityAmount.ToString("C");

                    //if (PersonalResponsibilityAmount < 0) txtPersonalResponsibility.BackColor = Color.Yellow;

                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Check")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                        }

                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "ACH/Banking")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                        }

                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Credit Card")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                        }
                        if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "None")
                        {
                            gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = true;
                            gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = true;
                        }
                    }

                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if (gvSettlementsInMedBill[2, i]?.Value?.ToString() == "Ineligible") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                        else
                        {
                            gvSettlementsInMedBill["IneligibleReason", i].Value = null;
                            gvSettlementsInMedBill["IneligibleReason", i].ReadOnly = true;
                        }
                    }

                    Decimal SettlementTotal = 0;
                    Decimal Balance = 0;
                    Decimal BillAmount = Decimal.Parse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"));
                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        Decimal Settlement = Decimal.Parse(gvSettlementsInMedBill["SettlementAmount", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                        SettlementTotal += Settlement;
                    }
                    if (SettlementTotal > BillAmount) MessageBox.Show("Settlement Total exceeds the Medical Bill Amount.", "Alert");
                    else
                    {
                        Balance = BillAmount - SettlementTotal;
                        txtBalance.Text = Balance.ToString("C");
                    }

                    btnAddNewSettlement.Enabled = true;
                    //btnEditSettlement.Enabled = true;
                    btnSaveSettlement.Enabled = true;
                    btnDeleteSettlement.Enabled = true;

                    tbCMMManager.TabPages.Insert(5, tbpgMedicalBill);
                    tbCMMManager.SelectedIndex = 5;
                }
              
            }
            else
            {
                MessageBox.Show("Medical Bill screen already open.");
            }
        }

        private void ClearGVSettlementSafely()
        {
            gvSettlementsInMedBill.BeginInvoke(new RemoveAllRowsInSettlement(RemoveAllRowSettlement));
        }

        private void AddNewRowToGVSettlementSafely(DataGridViewRow row)
        {
            gvSettlementsInMedBill.BeginInvoke(new AddRowToGVSettlement(AddRowToSettlement), row);
        }

        private void AddRowToSettlement(DataGridViewRow row)
        {
            gvSettlementsInMedBill.Rows.Add(row);
        }

        private void RemoveAllRowSettlement()
        {
            gvSettlementsInMedBill.Rows.Clear();
        }

        private void RemoveRowSettlement(int i)
        {
            gvSettlementsInMedBill.Rows.RemoveAt(i);
        }

        /// <summary>
        /// Modify this method
        /// </summary>
        private void ClearMedBillInCaseSafely()
        {
            gvCasePageMedBills.BeginInvoke(new RemoveAllMedBillInCase(RemoveAllMedBills));
        }

        private void AddNewRowToMedBillInCaseSafely(DataGridViewRow row)
        {
            gvCasePageMedBills.BeginInvoke(new AddRowToMedBillInCase(AddRowToMedBill), row);
        }

        private void SetMedBillBalanceSafely(Decimal balance)
        {
            txtBalance.BeginInvoke(new SetBalaceMedBill(SetMedBillBalance), balance);
        }

        private void AddRowToMedBill(DataGridViewRow row)
        {
            gvCasePageMedBills.Rows.Add(row);
        }

        private void RemoveRowMedBill(int i)
        {
            gvCasePageMedBills.Rows.RemoveAt(i);
        }

        private void RemoveAllMedBills()
        {
            gvCasePageMedBills.Rows.Clear();
        }



        private void SetTabPageCMMManagerSafely(int nIndex)
        {
            tbCMMManager.BeginInvoke(new SetTabPages(SetTabPage), nIndex);
        }

        private void SetTabPage(int nIndex)
        {
            tbCMMManager.SelectedIndex = nIndex;
        }

        private void SetMedBillBalance(Decimal Balance)
        {
            txtBalance.Text = Balance.ToString("C");
        }

        private void OnSettlementsInMedBillEditChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnSettlementsInMedBillEditChange;

                UpdateGridViewSettlementsInMedBillEdit();
            }
        }

        private void UpdateGridViewSettlementsInMedBillEdit()
        {

            String MedBillNo = txtMedBillNo.Text.Trim();
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            String strSqlQueryForSettlement = "select [dbo].[tbl_settlement].[Name], [dbo].[tbl_settlement_type_code].[SettlementTypeValue], [dbo].[tbl_settlement].[Amount], " +
                                  "[dbo].[tbl_settlement].[PersonalResponsibilityCredit], [dbo].[tbl_payment_method].[PaymentMethod_Value], " +
                                  "[dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate], " +
                                  "[dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
                                  "[dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], [dbo].[tbl_settlement].[ACH_Reconciled], " +
                                  "[dbo].[tbl_Credit_Card__c].[Name], [dbo].[tbl_settlement].[CMMCreditCardPaidDate], [dbo].[tbl_settlement].[CC_Reconciled], " +
                                  "[dbo].[tbl_settlement].[AllowedAmount], [dbo].[tbl_settlement].[IneligibleReason], [dbo].[tbl_settlement].[Notes] " +
                                  "from [dbo].[tbl_settlement] inner join [dbo].[tbl_settlement_type_code] " +
                                  "on [dbo].[tbl_settlement].[SettlementType] = [dbo].[tbl_settlement_type_code].[SettlementTypeCode] " +
                                  "inner join [dbo].[tbl_payment_method] on [dbo].[tbl_settlement].[CMMPaymentMethod] = [dbo].[tbl_payment_method].[PaymentMethod_Id] " +
                                  "inner join [dbo].[tbl_Credit_Card__c] on [dbo].[tbl_settlement].[CMMCreditCard] = [dbo].[tbl_Credit_Card__c].[CreditCard_Id]" +
                                  "where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillNo and [dbo].[tbl_settlement].[IsDeleted] = 0 " +
                                  "order by [dbo].[tbl_settlement].[Name]";

            SqlCommand cmdQueryForSettlement = new SqlCommand(strSqlQueryForSettlement, connRN);
            cmdQueryForSettlement.CommandType = CommandType.Text;

            cmdQueryForSettlement.Parameters.AddWithValue("@MedBillNo", MedBillNo);

            SqlDependency dependencySettlementInMedBill = new SqlDependency(cmdQueryForSettlement);
            dependencySettlementInMedBill.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillEditChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrSettlement = cmdQueryForSettlement.ExecuteReader();

            if (IsHandleCreated) ClearGVSettlementSafely();
            else gvSettlementsInMedBill.Rows.Clear();

            if (rdrSettlement.HasRows)
            {
                // has to put code to clear all rows in gvSettlementsInMedBill
                while (rdrSettlement.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();
                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    if (!rdrSettlement.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(0) });
                    if (!rdrSettlement.IsDBNull(1))
                    {
                        DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
                        for (int i = 0; i < lstSettlementType.Count; i++)
                        {
                            comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                        }
                        for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                        {
                            if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                        }
                        row.Cells.Add(comboCellSettlementType);
                    }
                    else
                    {
                        DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
                        for (int i = 0; i < lstSettlementType.Count; i++)
                        {
                            comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
                        }

                        for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
                        {
                            if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
                                comboCellSettlementType.Value = comboCellSettlementType.Items[i];
                        }

                        row.Cells.Add(comboCellSettlementType);
                    }

                    if (!rdrSettlement.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(2).ToString("C") });

                    else
                    {
                        Decimal Zero = 0;
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                    }


                    if (!rdrSettlement.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(3).ToString("C") });
                    else
                    {
                        Decimal Zero = 0;
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
                    }

                    /////////////////////////////////////////////////////////////////////
                    if (!rdrSettlement.IsDBNull(4))
                    {
                        DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                        for (int i = 0; i < lstPaymentMethod.Count; i++)
                        {
                            if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                            else comboCellPaymentMethod.Items.Add(String.Empty);
                        }

                        for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
                        {
                            if (rdrSettlement.GetString(4) == comboCellPaymentMethod.Items[i].ToString())
                                comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
                        }

                        row.Cells.Add(comboCellPaymentMethod);
                    }
                    else
                    {
                        DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

                        for (int i = 0; i < lstPaymentMethod.Count; i++)
                        {
                            if (lstPaymentMethod[i].PaymentMethodValue != null) comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
                            else comboCellPaymentMethod.Items.Add(String.Empty);
                        }

                        comboCellPaymentMethod.Value = null;

                        row.Cells.Add(comboCellPaymentMethod);

                    }


                    /////////////////////////////////////////////////////////////////////
                    if (!rdrSettlement.IsDBNull(5))
                    {

                        DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                        approvedCell.Value = rdrSettlement.GetBoolean(5);
                        approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        row.Cells.Add(approvedCell);
                    }
                    else
                    {
                        DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
                        approvedCell.Value = false;
                        approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        row.Cells.Add(approvedCell);
                    }

                    if (!rdrSettlement.IsDBNull(6)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(6).ToString("MM/dd/yyyy")) });
                    else row.Cells.Add(new CalendarCell { Value = null });

                    // Payment information
                    if (!rdrSettlement.IsDBNull(4))
                    {
                        String strPaymentMethod = rdrSettlement.GetString(4);

                        switch (strPaymentMethod)
                        {
                            case "Check":
                                if (!rdrSettlement.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(7) });
                                else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                //row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                DataGridViewComboBoxCell comboCellCreditCardNoneForCheck = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                {
                                    if (lstCreditCardInfo[i].CreditCardNo != null)
                                        comboCellCreditCardNoneForCheck.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                    else comboCellCreditCardNoneForCheck.Items.Add(String.Empty);
                                }
                                row.Cells.Add(comboCellCreditCardNoneForCheck);

                                if (!rdrSettlement.IsDBNull(8)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(8).ToString("MM/dd/yyyy")) });
                                if (!rdrSettlement.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(9) });
                                break;
                            case "ACH/Banking":
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                if (!rdrSettlement.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(10) });
                                else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                                DataGridViewComboBoxCell comboCellCreditCardNoneForACH = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                {
                                    if (lstCreditCardInfo[i].CreditCardNo != null)
                                        comboCellCreditCardNoneForACH.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                    else comboCellCreditCardNoneForACH.Items.Add(String.Empty);
                                }
                                row.Cells.Add(comboCellCreditCardNoneForACH);

                                //row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                if (!rdrSettlement.IsDBNull(11)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(11).ToString("MM/dd/yyyy")) });
                                if (!rdrSettlement.IsDBNull(12)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(12) });
                                break;
                            case "Credit Card":
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                if (!rdrSettlement.IsDBNull(13))
                                {
                                    DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                    {
                                        if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                        else comboCellCreditCard.Items.Add(String.Empty);
                                    }
                                    for (int i = 0; i < comboCellCreditCard.Items.Count; i++)
                                    {
                                        if (rdrSettlement.GetString(13) == comboCellCreditCard.Items[i].ToString())
                                            comboCellCreditCard.Value = comboCellCreditCard.Items[i];
                                    }
                                    row.Cells.Add(comboCellCreditCard);
                                }
                                else
                                {
                                    DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();
                                    for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                    {
                                        if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                        else comboCellCreditCard.Items.Add(String.Empty);
                                    }
                                    row.Cells.Add(comboCellCreditCard);
                                }
                                if (!rdrSettlement.IsDBNull(14)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(14).ToString("MM/dd/yyyy")) });
                                if (!rdrSettlement.IsDBNull(15)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(15) });
                                break;
                            default:
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
                                DataGridViewComboBoxCell comboCellCreditCardNone = new DataGridViewComboBoxCell();
                                for (int i = 0; i < lstCreditCardInfo.Count; i++)
                                {
                                    if (lstCreditCardInfo[i].CreditCardNo != null) comboCellCreditCardNone.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                                    else comboCellCreditCardNone.Items.Add(String.Empty);
                                }
                                row.Cells.Add(comboCellCreditCardNone);
                                row.Cells.Add(new CalendarCell { Value = null });
                                row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });
                                break;
                        }
                    }
                    else
                    {
                        DataGridViewTextBoxCell txtCheckNoCell = new DataGridViewTextBoxCell();
                        txtCheckNoCell.Value = null;
                        row.Cells.Add(txtCheckNoCell);
                        DataGridViewTextBoxCell txtACHNoCell = new DataGridViewTextBoxCell();
                        txtACHNoCell.Value = null;
                        row.Cells.Add(txtACHNoCell);
                        DataGridViewComboBoxCell comboCreditCardCell = new DataGridViewComboBoxCell();
                        for (int i = 0; i < lstCreditCardInfo.Count; i++)
                        {
                            if (lstCreditCardInfo[i].CreditCardNo != null) comboCreditCardCell.Items.Add(lstCreditCardInfo[i].CreditCardNo);
                            else comboCreditCardCell.Items.Add(String.Empty);
                        }
                        row.Cells.Add(comboCreditCardCell);
                        comboCreditCardCell.ReadOnly = true;
                        CalendarCell calPaymentDate = new CalendarCell();
                        calPaymentDate.Value = null;
                        row.Cells.Add(calPaymentDate);
                        DataGridViewCheckBoxCell chkReconciledCell = new DataGridViewCheckBoxCell();
                        chkReconciledCell.Value = false;
                        row.Cells.Add(chkReconciledCell);
                    }

                    if (!rdrSettlement.IsDBNull(16)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(16).ToString("C") });
                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                    if (!rdrSettlement.IsDBNull(17))
                    {
                        if (dicIneligibleReason.Count > 0)
                        {
                            DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                            for (int i = 0; i < dicIneligibleReason.Count; i++)
                            {
                                comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                            }
                            comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[rdrSettlement.GetInt32(17)];
                            row.Cells.Add(comboCellIneligibleReason);
                        }
                    }
                    else
                    {
                        if (dicIneligibleReason.Count > 0)
                        {
                            DataGridViewComboBoxCell comboCellIneligibleReason = new DataGridViewComboBoxCell();
                            for (int i = 0; i < dicIneligibleReason.Count; i++)
                            {
                                comboCellIneligibleReason.Items.Add(dicIneligibleReason[i]);
                            }
                            comboCellIneligibleReason.Value = comboCellIneligibleReason.Items[0];
                            row.Cells.Add(comboCellIneligibleReason);
                        }
                    }


                    if (!rdrSettlement.IsDBNull(18)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(18) });
                    else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });

                    if (IsHandleCreated) AddNewRowToGVSettlementSafely(row);
                    else gvSettlementsInMedBill.Rows.Add(row);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            //{
            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Check")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //    }

            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "ACH/Banking")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //    }

            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Credit Card")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //    }
            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == String.Empty)
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["PaymentDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["Reconciled"].ReadOnly = true;
            //    }
            //}

            Decimal SettlementTotal = 0;
            Decimal Balance = 0;
            Decimal BillAmount = Decimal.Parse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"));
            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                Decimal Settlement = Decimal.Parse(gvSettlementsInMedBill["SettlementAmount", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                SettlementTotal += Settlement;
            }
            if (SettlementTotal > BillAmount) MessageBox.Show("Settlement Total exceeds the Medical Bill Amount.", "Alert");
            else
            {
                Balance = BillAmount - SettlementTotal;
                SetMedBillBalanceSafely(Balance);
              
            }

            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if (gvSettlementsInMedBill[2, i]?.Value?.ToString() == "Ineligible") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                else
                {
                    gvSettlementsInMedBill["IneligibleReason", i].Value = null;
                    gvSettlementsInMedBill["IneligibleReason", i].ReadOnly = true;
                }
            }

            btnAddNewSettlement.Enabled = true;
            //btnEditSettlement.Enabled = true;
            btnSaveSettlement.Enabled = true;
            btnDeleteSettlement.Enabled = true;

            //tbCMMManager.TabPages.Insert(4, tbpgMedicalBill);
            //tbCMMManager.SelectedIndex = 4;
            SetTabPageCMMManagerSafely(5);




            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///
            //String strSqlQueryForSettlement = "select [dbo].[tbl_settlement].[Name], [dbo].[tbl_settlement_type_code].[SettlementTypeValue], [dbo].[tbl_settlement].[Amount], " +
            //                      "[dbo].[tbl_settlement].[PersonalResponsibilityCredit], [dbo].[tbl_settlement].[Approved], [dbo].[tbl_settlement].[ApprovedDate], " +
            //                      "[dbo].[tbl_payment_method].[PaymentMethod_Value], " +
            //                      "[dbo].[tbl_settlement].[CheckNo], [dbo].[tbl_settlement].[CheckDate], [dbo].[tbl_settlement].[CheckReconciled], " +
            //                      "[dbo].[tbl_settlement].[ACH_Number], [dbo].[tbl_settlement].[ACH_Date], [dbo].[tbl_settlement].[ACH_Reconciled], " +
            //                      "[dbo].[tbl_Credit_Card__c].[Name], [dbo].[tbl_settlement].[CMMCreditCardPaidDate], [dbo].[tbl_settlement].[CC_Reconciled], " +
            //                      "[dbo].[tbl_settlement].[AllowedAmount], [dbo].[tbl_settlement].[Notes] " +
            //                      "from [dbo].[tbl_settlement] inner join [dbo].[tbl_settlement_type_code] " +
            //                      "on [dbo].[tbl_settlement].[SettlementType] = [dbo].[tbl_settlement_type_code].[SettlementTypeCode] " +
            //                      "inner join [dbo].[tbl_payment_method] on [dbo].[tbl_settlement].[CMMPaymentMethod] = [dbo].[tbl_payment_method].[PaymentMethod_Id] " +
            //                      "inner join [dbo].[tbl_Credit_Card__c] on [dbo].[tbl_settlement].[CMMCreditCard] = [dbo].[tbl_Credit_Card__c].[CreditCard_Id]" +
            //                      "where [dbo].[tbl_settlement].[MedicalBillID] = @MedBillNo and [dbo].[tbl_settlement].[IsDeleted] = 0 " +
            //                      "order by [dbo].[tbl_settlement].[Name]";

            //SqlCommand cmdQueryForSettlement = new SqlCommand(strSqlQueryForSettlement, connRN);
            //cmdQueryForSettlement.CommandType = CommandType.Text;

            //cmdQueryForSettlement.Parameters.AddWithValue("@MedBillNo", MedBillNo);

            ////SqlDependency dependencySettlementInMedBill = new SqlDependency(cmdQueryForSettlement);
            ////dependencySettlementInMedBill.OnChange += new OnChangeEventHandler(OnSettlementsInMedBillEditChange);

            //gvSettlementsInMedBill.Columns["SettlementAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            //gvSettlementsInMedBill.Columns["PersonalResponsibility"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            //gvSettlementsInMedBill.Columns["AllowedAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            //connRN.Open();
            //SqlDataReader rdrSettlement = cmdQueryForSettlement.ExecuteReader();

            ////while (gvSettlementsInMedBill.Rows.Count > 0)
            ////{
            ////    gvSettlementsInMedBill.Rows.RemoveAt(0);
            ////}

            //gvSettlementsInMedBill.Rows.Clear();
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////
            //if (rdrSettlement.HasRows)
            //{
            //    while (rdrSettlement.Read())
            //    {

            //        DataGridViewRow row = new DataGridViewRow();
            //        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
            //        if (!rdrSettlement.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(0) });
            //        if (!rdrSettlement.IsDBNull(1))
            //        {
            //            DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();

            //            for (int i = 0; i < lstSettlementType.Count; i++)
            //            {
            //                comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
            //            }
            //            for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
            //            {
            //                if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
            //                    comboCellSettlementType.Value = comboCellSettlementType.Items[i];
            //            }

            //            row.Cells.Add(comboCellSettlementType);
            //        }
            //        else
            //        {
            //            DataGridViewComboBoxCell comboCellSettlementType = new DataGridViewComboBoxCell();
            //            for (int i = 0; i < lstSettlementType.Count; i++)
            //            {
            //                comboCellSettlementType.Items.Add(lstSettlementType[i].SettlementTypeValue);
            //            }

            //            for (int i = 0; i < comboCellSettlementType.Items.Count; i++)
            //            {
            //                if (rdrSettlement.GetString(1) == comboCellSettlementType.Items[i].ToString())
            //                    comboCellSettlementType.Value = comboCellSettlementType.Items[i];
            //            }

            //            row.Cells.Add(comboCellSettlementType);
            //        }

            //        if (!rdrSettlement.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(2).ToString("C") });
            //        else
            //        {
            //            Decimal Zero = 0;
            //            row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
            //        }


            //        if (!rdrSettlement.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(3).ToString("C") });
            //        else
            //        {
            //            Decimal Zero = 0;
            //            row.Cells.Add(new DataGridViewTextBoxCell { Value = Zero.ToString("C") });
            //        }

            //        if (!rdrSettlement.IsDBNull(4))
            //        {

            //            DataGridViewCheckBoxCell approvedCell = new DataGridViewCheckBoxCell();
            //            approvedCell.Value = rdrSettlement.GetBoolean(4);
            //            approvedCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //            row.Cells.Add(approvedCell);
            //        }
            //        else row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });

            //        if (!rdrSettlement.IsDBNull(5)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(5).ToString("MM/dd/yyyy")) });
            //        else row.Cells.Add(new CalendarCell { Value = null });


            //        /////////////////////////////

            //        if (!rdrSettlement.IsDBNull(6))
            //        {
            //            DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

            //            for (int i = 0; i < lstPaymentMethod.Count; i++)
            //            {
            //                comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
            //            }

            //            for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
            //            {
            //                if (rdrSettlement.GetString(6) == comboCellPaymentMethod.Items[i].ToString())
            //                    comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
            //            }

            //            row.Cells.Add(comboCellPaymentMethod);
            //        }
            //        else
            //        {
            //            DataGridViewComboBoxCell comboCellPaymentMethod = new DataGridViewComboBoxCell();

            //            for (int i = 0; i < lstPaymentMethod.Count; i++)
            //            {
            //                comboCellPaymentMethod.Items.Add(lstPaymentMethod[i].PaymentMethodValue);
            //            }

            //            for (int i = 0; i < comboCellPaymentMethod.Items.Count; i++)
            //            {
            //                if (rdrSettlement.GetString(6) == comboCellPaymentMethod.Items[i].ToString())
            //                    comboCellPaymentMethod.Value = comboCellPaymentMethod.Items[i];
            //            }

            //            row.Cells.Add(comboCellPaymentMethod);

            //        }

            //        if (!rdrSettlement.IsDBNull(7)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(7) });
            //        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(8)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(8).ToString("MM/dd/yyyy")) });
            //        else row.Cells.Add(new CalendarCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(9) });
            //        else row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(10) });
            //        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(11)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(11).ToString("MM/dd/yyyy")) });
            //        else row.Cells.Add(new CalendarCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(12)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrSettlement.GetBoolean(12) });
            //        else row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });

            //        if (!rdrSettlement.IsDBNull(13))
            //        {
            //            DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();

            //            for (int i = 0; i < lstCreditCardInfo.Count; i++)
            //            {
            //                comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
            //            }

            //            for (int i = 0; i < comboCellCreditCard.Items.Count; i++)
            //            {
            //                if (rdrSettlement.GetString(13) == comboCellCreditCard.Items[i].ToString())
            //                    comboCellCreditCard.Value = comboCellCreditCard.Items[i];
            //            }

            //            row.Cells.Add(comboCellCreditCard);
            //        }
            //        else
            //        {
            //            DataGridViewComboBoxCell comboCellCreditCard = new DataGridViewComboBoxCell();

            //            for (int i = 0; i < lstCreditCardInfo.Count; i++)
            //            {
            //                comboCellCreditCard.Items.Add(lstCreditCardInfo[i].CreditCardNo);
            //            }

            //            row.Cells.Add(comboCellCreditCard);
            //        }

            //        if (!rdrSettlement.IsDBNull(14)) row.Cells.Add(new CalendarCell { Value = DateTime.Parse(rdrSettlement.GetDateTime(14).ToString("MM/dd/yyyy")) });
            //        else row.Cells.Add(new CalendarCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(15))
            //        {
            //            DataGridViewCheckBoxCell ccReconciledCell = new DataGridViewCheckBoxCell();
            //            ccReconciledCell.Value = rdrSettlement.GetBoolean(15);
            //            ccReconciledCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            //            row.Cells.Add(ccReconciledCell);
            //        }
            //        else row.Cells.Add(new DataGridViewCheckBoxCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(16)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetDecimal(16).ToString("C") });
            //        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });
            //        if (!rdrSettlement.IsDBNull(17)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrSettlement.GetString(17) });
            //        else row.Cells.Add(new DataGridViewTextBoxCell { Value = null });


            //        gvSettlementsInMedBill.Rows.Add(row);
            //    }
            //}
            //connRN.Close();

            //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            //{
            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Check")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckDate"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckReconciled"].ReadOnly = false;

            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Date"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Reconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CMMCreditCardPaidDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CC_Reconciled"].ReadOnly = true;
            //    }

            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "ACH/Banking")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckReconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Date"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Reconciled"].ReadOnly = false;

            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CMMCreditCardPaidDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CC_Reconciled"].ReadOnly = true;

            //    }

            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "Credit Card")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckReconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Date"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Reconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CMMCreditCardPaidDate"].ReadOnly = false;
            //        gvSettlementsInMedBill.Rows[i].Cells["CC_Reconciled"].ReadOnly = false;
            //    }
            //    if (gvSettlementsInMedBill["PaymentMethod", i]?.Value?.ToString() == "None")
            //    {
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CheckReconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["ACHNo"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Date"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["ACH_Reconciled"].ReadOnly = true;

            //        gvSettlementsInMedBill.Rows[i].Cells["CreditCard"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CMMCreditCardPaidDate"].ReadOnly = true;
            //        gvSettlementsInMedBill.Rows[i].Cells["CC_Reconciled"].ReadOnly = true;
            //    }
            //}

            //for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            //{
            //    if (gvSettlementsInMedBill[2, i]?.Value?.ToString() == "Ineligible") gvSettlementsInMedBill.Rows[i].DefaultCellStyle.BackColor = Color.Red;
            //}

            //btnAddNewSettlement.Enabled = true;
            //btnEditSettlement.Enabled = true;
            //btnSaveSettlement.Enabled = true;
            //btnDeleteSettlement.Enabled = true;

            //tbCMMManager.SelectedIndex = 4;
        }

        private void btnDeleteCase_Click(object sender, EventArgs e)
        {

            DialogResult dlgResult = MessageBox.Show("Are you sure to delete these cases?", "Alert", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                List<CaseInfo> lstCaseInfoToDelete = new List<CaseInfo>();

                for (int i = 0; i < gvCaseViewCaseHistory.Rows.Count; i++)
                {
                    if ((Boolean)gvCaseViewCaseHistory[0, i].Value == true)
                    {
                        lstCaseInfoToDelete.Add(new CaseInfo { CaseName = gvCaseViewCaseHistory[1, i].Value.ToString(), IndividualId = txtCaseHistoryIndividualID.Text.Trim() });
                    }
                }

                if (lstCaseInfoToDelete.Count > 0)
                {
                    try
                    {
                        Boolean bError = false;
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlTransaction transDelete = connRN.BeginTransaction();

                        for (int i = 0; i < lstCaseInfoToDelete.Count; i++)
                        {
                            String strSqlDeleteCaseSelected = "update [dbo].[tbl_case] set [dbo].[tbl_case].[IsDeleted] = 1 " +
                                                              "where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualId";

                            SqlCommand cmdDeleteCaseSelected = new SqlCommand(strSqlDeleteCaseSelected, connRN, transDelete);
                            cmdDeleteCaseSelected.CommandType = CommandType.Text;

                            cmdDeleteCaseSelected.Parameters.AddWithValue("@CaseName", lstCaseInfoToDelete[i].CaseName);
                            cmdDeleteCaseSelected.Parameters.AddWithValue("@IndividualId", lstCaseInfoToDelete[i].IndividualId);

                            //if (connRN.State == ConnectionState.Closed) connRN.Open();

                            int nRowDeleted = cmdDeleteCaseSelected.ExecuteNonQuery();
                            if (nRowDeleted == 0) bError = true;

                        }
                        transDelete.Commit();
                        //if (bError == true) MessageBox.Show("Some of cases have not been deleted.", "Error");
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                    finally
                    {
                        connRN.Close();
                    }
                }
                else MessageBox.Show("No case is selected");
            }
            else return;
        }

        private void btnDeleteSettlement_Click(object sender, EventArgs e)
        {

            List<String> lstSettlements = new List<String>();
            for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
            {
                if ((Boolean)gvSettlementsInMedBill[0, i]?.Value == true) lstSettlements.Add(gvSettlementsInMedBill[1, i]?.Value?.ToString());
            }

            if (lstSettlements.Count == 0)
            {
                MessageBox.Show("You have not selected settlement to delete");
                return;
            }

            DialogResult dr = MessageBox.Show("Are you sure to delete selected settlements?", "Alert", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)     // delete settlement
            {
                String MedBillNo = txtMedBillNo.Text.Trim();
                int nSettlementToDelete = lstSettlements.Count;
                int nDeletedSettlements = 0;

                foreach (String settlement in lstSettlements)
                {

                    String strSqlQueryForSettlement = "select [dbo].[tbl_settlement].[Name] from [dbo].[tbl_settlement] where [dbo].[tbl_settlement].[Name] = @SettlementNo";

                    SqlCommand cmdQueryForSettlement = new SqlCommand(strSqlQueryForSettlement, connRN);
                    cmdQueryForSettlement.CommandType = CommandType.Text;

                    cmdQueryForSettlement.Parameters.AddWithValue("@SettlementNo", settlement);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String Settlement = cmdQueryForSettlement.ExecuteScalar()?.ToString();
                    Object objSettlement = cmdQueryForSettlement.ExecuteScalar();

                    String Settlement = String.Empty;
                    if (objSettlement != null) Settlement = objSettlement.ToString();

                    if (connRN.State == ConnectionState.Open) connRN.Close();
                    if (Settlement == String.Empty)
                    {
                        for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                        {
                            if (settlement == gvSettlementsInMedBill[1, i]?.Value?.ToString())
                            {
                                gvSettlementsInMedBill.Rows.RemoveAt(i);
                                nDeletedSettlements++;
                            }
                        }
                    }
                    else
                    {
                        String strSqlUpdateSettlement = "update [dbo].[tbl_settlement] set [dbo].[tbl_settlement].[IsDeleted] = 1 " +
                                                        "where [dbo].[tbl_settlement].[Name] = @SettlementNo and " +
                                                        "[dbo].[tbl_settlement].[MedicalBillID] = @MedicalBillNo";

                        SqlCommand cmdUpdateSettlement = new SqlCommand(strSqlUpdateSettlement, connRN);
                        cmdUpdateSettlement.CommandType = CommandType.Text;

                        cmdUpdateSettlement.Parameters.AddWithValue("@SettlementNo", settlement);
                        cmdUpdateSettlement.Parameters.AddWithValue("@MedicalBillNo", MedBillNo);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nRowAffected = cmdUpdateSettlement.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        if (nRowAffected == 1) nDeletedSettlements++;
                    }
                }

                for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                {
                    if (gvSettlementsInMedBill["SettlementTypeValue", i]?.Value?.ToString() != "Ineligible")
                    {
                        gvSettlementsInMedBill["IneligibleReason", i].Value = null;
                        gvSettlementsInMedBill["IneligibleReason", i].ReadOnly = true;
                    }
                }

                if (nDeletedSettlements == nSettlementToDelete)
                {
                    MessageBox.Show("The settlements have been deleted.");
                }
                else MessageBox.Show("Some of settlements have not been deleted.");
            }
            else if (dr == DialogResult.No)     // do not delete settlement
            {
                return;
            }
        }

        private void btnEditCaseUnderProcess_Click(object sender, EventArgs e)
        {
            if (tbCMMManager.TabPages.Contains(tbpgCreateCase))
            {
                MessageBox.Show("The Case page is already opened.", "Alert", MessageBoxButtons.OK);
                return;
            }
            else
            {
                if (gvProcessingCaseNo.Rows.Count > 0)
                {
                    int nSelected = 0;
                    int nRowSelected = 0;
                    for (int i = 0; i < gvProcessingCaseNo.Rows.Count; i++)
                    {
                        if ((Boolean)gvProcessingCaseNo["CaseSelected", i].Value == true)
                        {
                            nRowSelected = i;
                            nSelected++;
                        }
                    }

                    if (nSelected == 0)
                    {
                        MessageBox.Show("No Case selected", "Alert", MessageBoxButtons.OK);
                        return;
                    }
                    else if (nSelected > 1)
                    {
                        MessageBox.Show("More than one case selected", "Alert", MessageBoxButtons.OK);
                        return;
                    }
                    else if (nSelected == 1)
                    {
                        String CaseIdForIndividual = gvProcessingCaseNo["CaseIdForIndividual", nRowSelected]?.Value?.ToString();
                        String IndividualIdForCase = txtIndividualID.Text.Trim();

                        String strSqlQueryForCase = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[Contact_ID], [dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[ModifiDate], " +
                                                    "[dbo].[tbl_case].[CreateStaff], [dbo].[tbl_case].[ModifiStaff], [dbo].[tbl_case].[Case_status], " +
                                                    "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Form_Destination_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
                                                    "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Form_Destination_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                    "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Form_Destination_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
                                                    "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], [dbo].[tbl_case].[MedRec_Form_Destination_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                    "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], [dbo].[tbl_case].[Unknown_Form_Destination_File_Name], [dbo].[tbl_case].[Unknown_Receiv_Date], " +
                                                    "[dbo].[tbl_case].[Case_status], [dbo].[tbl_case].[Note] " +
                                                    "from [dbo].[tbl_case] where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualID";

                        SqlCommand cmdQueryForCase = new SqlCommand(strSqlQueryForCase, connRN);
                        cmdQueryForCase.CommandType = CommandType.Text;

                        cmdQueryForCase.Parameters.AddWithValue("@CaseName", CaseIdForIndividual);
                        cmdQueryForCase.Parameters.AddWithValue("@IndividualID", IndividualIdForCase);

                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlDataReader rdrCaseForIndividual = cmdQueryForCase.ExecuteReader();
                        if (rdrCaseForIndividual.HasRows)
                        {
                            rdrCaseForIndividual.Read();

                            txtCaseName.Text = rdrCaseForIndividual.GetString(0);
                            txtCaseIndividualID.Text = rdrCaseForIndividual.GetString(1);

                            // NPF Form
                            if (rdrCaseForIndividual.GetBoolean(7) == true) chkNPF_CaseCreationPage.Checked = true;
                            if (!rdrCaseForIndividual.IsDBNull(8)) txtNPFFormFilePath.Text = rdrCaseForIndividual.GetString(8);
                            if (!rdrCaseForIndividual.IsDBNull(9)) strNPFormFilePathDestination = rdrCaseForIndividual.GetString(9);
                            if (!rdrCaseForIndividual.IsDBNull(10)) txtNPFUploadDate.Text = rdrCaseForIndividual.GetDateTime(10).ToString("MM/dd/yyyy");

                            // IB Form
                            if (rdrCaseForIndividual.GetBoolean(11) == true) chkIB_CaseCreationPage.Checked = true;
                            if (!rdrCaseForIndividual.IsDBNull(12)) txtIBFilePath.Text = rdrCaseForIndividual.GetString(12);
                            if (!rdrCaseForIndividual.IsDBNull(13)) strIBFilePathDestination = rdrCaseForIndividual.GetString(13);
                            if (!rdrCaseForIndividual.IsDBNull(14)) txtIBUploadDate.Text = rdrCaseForIndividual.GetDateTime(14).ToString("MM/dd/yyyy");

                            // POP Form
                            if (rdrCaseForIndividual.GetBoolean(15) == true) chkPoP_CaseCreationPage.Checked = true;
                            if (!rdrCaseForIndividual.IsDBNull(16)) txtPopFilePath.Text = rdrCaseForIndividual.GetString(16);
                            if (!rdrCaseForIndividual.IsDBNull(17)) strPopFilePathDestination = rdrCaseForIndividual.GetString(17);
                            if (!rdrCaseForIndividual.IsDBNull(18)) txtPoPUploadDate.Text = rdrCaseForIndividual.GetDateTime(18).ToString("MM/dd/yyyy");

                            // Med Rec Form
                            if (rdrCaseForIndividual.GetBoolean(19) == true) chkMedicalRecordCaseCreationPage.Checked = true;
                            if (!rdrCaseForIndividual.IsDBNull(20)) txtMedicalRecordFilePath.Text = rdrCaseForIndividual.GetString(20);
                            if (!rdrCaseForIndividual.IsDBNull(21)) strMedRecordFilePathDestination = rdrCaseForIndividual.GetString(21);
                            if (!rdrCaseForIndividual.IsDBNull(22)) txtMRUploadDate.Text = rdrCaseForIndividual.GetDateTime(22).ToString("MM/dd/yyyy");

                            // Unknown Doc Form
                            if (rdrCaseForIndividual.GetBoolean(23) == true) chkOtherDocCaseCreationPage.Checked = true;
                            if (!rdrCaseForIndividual.IsDBNull(24)) txtOtherDocumentFilePath.Text = rdrCaseForIndividual.GetString(24);
                            if (!rdrCaseForIndividual.IsDBNull(25)) strUnknownDocFilePathDestination = rdrCaseForIndividual.GetString(25);
                            if (!rdrCaseForIndividual.IsDBNull(26)) txtOtherDocUploadDate.Text = rdrCaseForIndividual.GetDateTime(26).ToString("MM/dd/yyyy");

                            // Case status
                            if (rdrCaseForIndividual.GetBoolean(27) == true) txtCaseStatus.Text = "Complete and Ready";
                            else txtCaseStatus.Text = "Pending - Additional Documents required";

                            // Note
                            if (!rdrCaseForIndividual.IsDBNull(28)) txtNoteOnCase.Text = rdrCaseForIndividual.GetString(28);

                            // Individual Name
                            tbCMMManager.TabPages.Insert(3, tbpgCaseView);
                            tbCMMManager.TabPages.Insert(4, tbpgCreateCase);
                            tbCMMManager.SelectedIndex = 4;
                        }
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        String strSqlQueryForMedBillInCase = "select [dbo].[tbl_medbill].[BillNo], [dbo].[tbl_medbill_type].[MedBillTypeName], " +
                                         "[dbo].[tbl_medbill].[CreatedDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                         "[dbo].[tbl_medbill].[LastModifiedDate], [dbo].[tbl_ModifiStaff].[Staff_Name], " +
                                         "[dbo].[tbl_medbill].[BillAmount], [dbo].[tbl_medbill].[SettlementTotal], [dbo].[tbl_medbill].[TotalSharedAmount], [dbo].[tbl_medbill].[Balance] " +
                                         "from [dbo].[tbl_medbill] " +
                                         "inner join [dbo].[tbl_medbill_type] on [dbo].[tbl_medbill].[MedBillType_Id] = [dbo].[tbl_medbill_type].[MedBillTypeId] " +
                                         "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_medbill].[CreatedById] = [dbo].[tbl_CreateStaff].[CreateStaff_Id] " +
                                         "inner join [dbo].[tbl_ModifiStaff] on [dbo].[tbl_medbill].[LastModifiedById] = [dbo].[tbl_ModifiStaff].[ModifiStaff_Id] " +
                                         "where [dbo].[tbl_medbill].[Case_Id] = @CaseName and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";



                        SqlCommand cmdQueryForMedBillsInCase = new SqlCommand(strSqlQueryForMedBillInCase, connRN);
                        cmdQueryForMedBillsInCase.CommandType = CommandType.Text;

                        cmdQueryForMedBillsInCase.Parameters.AddWithValue("@CaseName", CaseIdForIndividual);
                        cmdQueryForMedBillsInCase.Parameters.AddWithValue("@IndividualId", IndividualIdForCase);

                        SqlDependency dependencyMedBillInCase = new SqlDependency(cmdQueryForMedBillsInCase);
                        dependencyMedBillInCase.OnChange += new OnChangeEventHandler(OnMedBillsInCaseChange);

                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();

                        SqlDataReader rdrMedBillInCase = cmdQueryForMedBillsInCase.ExecuteReader();

                        gvCasePageMedBills.Rows.Clear();
                        if (rdrMedBillInCase.HasRows)
                        {
                            while (rdrMedBillInCase.Read())
                            {
                                DataGridViewRow row = new DataGridViewRow();

                                row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(0) });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(1) });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(2).ToString("MM/dd/yyyy") });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(3) });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDateTime(4).ToString("MM/dd/yyyy") });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetString(5) });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(6).ToString("C") });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(7).ToString("C") });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(8).ToString("C") });
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrMedBillInCase.GetDecimal(9).ToString("C") });

                                gvCasePageMedBills.Rows.Add(row);
                            }
                        }

                        if (connRN.State == ConnectionState.Open) connRN.Close();

                    }
                }
            }
        }

        private void btnMedicalProviderInfo_Click(object sender, EventArgs e)
        {
            frmMedicalProviderInfo frmMedProviderInfo = new frmMedicalProviderInfo();

            frmMedProviderInfo.StartPosition = FormStartPosition.CenterParent;
            frmMedProviderInfo.ShowDialog();
        }

        private void frmCMMManager_Shown(object sender, EventArgs e)
        {
            //txtMedBillGuarantor.TextChanged += new EventHandler(txtMedBillGuarantor_TextChanged);
            //txtMedBill_Illness.TextChanged += new EventHandler(txtMedBill_Illness_TextChanged);
            //txtMedBill_Incident.TextChanged += new EventHandler(txtMedBill_Incident_TextChanged);
            //txtMedBillAmount.TextChanged += new EventHandler(txtMedBillAmount_TextChanged);
            //txtBalance.TextChanged += new EventHandler(txtBalance_TextChanged);
            //txtPrescriptionName.TextChanged += new EventHandler(txtPrescriptionName_TextChanged);
            //txtPrescriptionNo.TextChanged += new EventHandler(txtPrescriptionNo_TextChanged);
            //txtPrescriptionDescription.TextChanged += new EventHandler(txtPrescriptionDescription_TextChanged);
            //txtNumPhysicalTherapy.TextChanged += new EventHandler(txtNumPhysicalTherapy_TextChanged);
            //cbMedicalBillNote1.SelectedIndexChanged += new EventHandler(cbMedicalBillNote1_SelectedIndexChanged);
            //cbMedicalBillNote2.SelectedIndexChanged += new EventHandler(cbMedicalBillNote2_SelectedIndexChanged);
            //cbMedicalBillNote3.SelectedIndexChanged += new EventHandler(cbMedicalBillNote3_SelectedIndexChanged);
            //cbMedicalBillNote4.SelectedIndexChanged += new EventHandler(cbMedicalBillNote4_SelectedIndexChanged);
            //txtMedicalBillNote1.TextChanged += new EventHandler(txtMedicalBillNote1_TextChanged);
            //txtMedicalBillNote2.TextChanged += new EventHandler(txtMedicalBillNote2_TextChanged);
            //txtMedicalBillNote3.TextChanged += new EventHandler(txtMedicalBillNote3_TextChanged);
            //txtMedicalBillNote4.TextChanged += new EventHandler(txtMedicalBillNote4_TextChanged);


            //dtpBillDate.ValueChanged += new EventHandler(dtpBillDate_ValueChanged);
            //dtpDueDate.ValueChanged += new EventHandler(dtpDueDate_ValueChanged);
        }

        private void txtMedBillAmount_TextChanged(object sender, EventArgs e)
        {
            TextBox txtAmount = (TextBox)sender;

            txtBalance.Text = txtAmount.Text;
        }

        private void comboMedBillType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem.ToString() == "Medical Bill")
            {
                lblPrescriptionName.Visible = false;
                lblPrescriptionDescription.Visible = false;
                lblPrescriptionNote.Visible = false;
                lblNumberOfMedication.Visible = false;

                txtPrescriptionName.Visible = false;
                txtPrescriptionDescription.Visible = false;
                txtPrescriptionNote.Visible = false;
                txtNumberOfMedication.Visible = false;

                lblNumberOfPhysicalTheraph.Visible = false;
                txtNumPhysicalTherapy.Visible = false;
                lblPhysicalTherapyRxNote.Visible = false;
                txtPhysicalTherapyRxNote.Visible = false;

                rbInpatient.Visible = true;
                rbOutpatient.Visible = true;

                lblMedBillNote.Visible = true;
                txtMedBillNote.Visible = true;

                lblPendingReason.Visible = true;
                comboPendingReason.Visible = true;

                lblIneligibleReason.Visible = true;
                comboIneligibleReason.Visible = true;
                
            }

            if (comboBox.SelectedItem.ToString() == "Prescription")
            {
                lblMedBillNote.Visible = false;
                txtMedBillNote.Visible = false;

                lblNumberOfPhysicalTheraph.Visible = false;
                txtNumPhysicalTherapy.Visible = false;

                lblPhysicalTherapyRxNote.Visible = false;
                txtPhysicalTherapyRxNote.Visible = false;

                lblPendingReason.Visible = false;
                comboPendingReason.Visible = false;

                lblIneligibleReason.Visible = false;
                comboIneligibleReason.Visible = false;

                rbInpatient.Visible = false;
                rbOutpatient.Visible = false;

                lblPrescriptionName.Visible = true;
                lblPrescriptionDescription.Visible = true;
                lblPrescriptionNote.Visible = true;
                lblNumberOfMedication.Visible = true;

                txtPrescriptionName.Visible = true;
                txtPrescriptionDescription.Visible = true;
                txtPrescriptionNote.Visible = true;
                txtNumberOfMedication.Visible = true;

            }

            if (comboBox.SelectedItem.ToString() == "Physical Therapy")
            {
                lblMedBillNote.Visible = false;
                txtMedBillNote.Visible = false;

                lblPrescriptionName.Visible = false;
                lblPrescriptionDescription.Visible = false;
                lblPrescriptionNote.Visible = false;
                lblNumberOfMedication.Visible = false;

                txtPrescriptionName.Visible = false;
                txtPrescriptionDescription.Visible = false;
                txtPrescriptionNote.Visible = false;
                txtNumberOfMedication.Visible = false;

                lblPendingReason.Visible = false;
                comboPendingReason.Visible = false;

                lblIneligibleReason.Visible = false;
                comboIneligibleReason.Visible = false;

                rbInpatient.Visible = false;
                rbOutpatient.Visible = false;

                lblNumberOfPhysicalTheraph.Visible = true;

                txtNumPhysicalTherapy.Visible = true;
                lblPhysicalTherapyRxNote.Visible = true;
                txtPhysicalTherapyRxNote.Visible = true;
                
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (gvIndividualSearched.Rows.Count > 0) gvIndividualSearched.DataSource = null;

            String strTextSearched = txtSearch.Text.Trim();

            //String strSqlSearchContact = "select [dbo].[contact].[Individual_ID__C] as [Individual No.], " +
            //                             "concat([dbo].[contact].[LastName], ', ', [dbo].[contact].[FirstName], ' ', [dbo].[contact].[MiddleName]) as Name, " +
            //                             "[dbo].[contact].[Social_Security_Number__c] as SSN, " +
            //                             "[dbo].[membership].[Name] as Membership, [dbo].[contact].[Legacy_Database_Individual_ID__C] as [CRM No.], " +
            //                             "[dbo].[contact].[c4g_Membership_Status__C] as [Membership Status], " +
            //                             "[dbo].[contact].[Membership_Ind_Start_Date__C] As [Membership Start Date], " +
            //                             "[dbo].[contact].[Membership_Cancelled_Date__C] As [Membership Cancel Date], " +
            //                             "[dbo].[contact].[BirthDate], [dbo].[contact].[cmm_Gender__C] as [Gender], " +
            //                             "[dbo].[contact].[Household_Role__C] as [House Type], " +
            //                             "[dbo].[program].[Name] as [Program Name], " +
            //                             "[dbo].[account].[BillingStreet], [dbo].[account].[BillingCity], [dbo].[account].[BillingState], [dbo].[account].[BillingPostalCode], " +
            //                             "[dbo].[account].[ShippingStreet], [dbo].[account].[ShippingCity], [dbo].[account].[ShippingState], [dbo].[account].[ShippingPostalCode], " +
            //                             "[dbo].[Church].[Name] as [Church Name], " +
            //                             "[dbo].[contact].[Email] " +
            //                             "from contact " +
            //                             "left join membership on contact.c4g_Membership__C = membership.ID " +
            //                             "left join account on contact.AccountID = account.ID " +
            //                             "left join program on contact.c4g_plan__c = program.ID " +
            //                             "left join Church on contact.c4g_Church__C = Church.ID " +
            //                             "where contact.LastName like '%' + @LastName + '%' or " +
            //                             "contact.FirstName like '%' + @FirstName + '%' or " +
            //                             "contact.Household_Role__C like '%' + @HouseholdRole + '%' or " +
            //                             "contact.c4g_Membership__C like '%' + @MembershipID + '%' or " +
            //                             "contact.c4g_Membership_Status__C like '%' + @MembershipStatus + '%' or " +
            //                             "contact.Social_Security_Number__C like '%' + @SSN + '%' or " +
            //                             "contact.Individual_ID__C like '%' + @IndividualID + '%' or " +
            //                             "contact.Legacy_Database_Individual_ID__C like '%' + @LagacyIndividualID + '%'";

            String strSqlSearchContact = "select [dbo].[contact].[Individual_ID__C] as [Individual No.], " +
                             "concat([dbo].[contact].[LastName], ', ', [dbo].[contact].[FirstName], ' ', [dbo].[contact].[MiddleName]) as Name, " +
                             "[dbo].[membership].[Name] as Membership, [dbo].[contact].[Legacy_Database_Individual_ID__C] as [CRM No.], " +
                             "[dbo].[contact].[c4g_Membership_Status__C] as [Membership Status], " +
                             "[dbo].[contact].[Membership_Ind_Start_Date__C] As [Membership Start Date], " +
                             "[dbo].[contact].[Membership_Cancelled_Date__C] As [Membership Cancel Date], " +
                             "[dbo].[contact].[BirthDate], [dbo].[contact].[cmm_Gender__C] as [Gender], " +
                             "[dbo].[contact].[Household_Role__C] as [House Type], " +
                             "[dbo].[program].[Name] as [Program Name], " +
                             "[dbo].[contact].[MailingStreet], [dbo].[contact].[MailingCity], [dbo].[contact].[MailingState], [dbo].[contact].[MailingPostalCode], " +
                             "[dbo].[contact].[OtherStreet], [dbo].[contact].[OtherCity], [dbo].[contact].[OtherState], [dbo].[contact].[OtherPostalCode], " +
                             "[dbo].[Church].[Name] as [Church Name], " +
                             "[dbo].[contact].[Email] " +
                             "from contact " +
                             "left join membership on contact.c4g_Membership__C = membership.ID " +
                             "left join account on contact.AccountID = account.ID " +
                             "left join program on contact.c4g_plan__c = program.ID " +
                             "left join Church on contact.c4g_Church__C = Church.ID " +
                             "where contact.LastName like '%' + @LastName + '%' or " +
                             "contact.FirstName like '%' + @FirstName + '%' or " +
                             "contact.Household_Role__C like '%' + @HouseholdRole + '%' or " +
                             "contact.c4g_Membership__C like '%' + @MembershipID + '%' or " +
                             "contact.c4g_Membership_Status__C like '%' + @MembershipStatus + '%' or " +
                             "contact.Social_Security_Number__C like '%' + @SSN + '%' or " +
                             "contact.Individual_ID__C like '%' + @IndividualID + '%' or " +
                             "contact.Legacy_Database_Individual_ID__C like '%' + @LagacyIndividualID + '%'";

            SqlCommand cmdQueryForIndividual = new SqlCommand(strSqlSearchContact, connSalesforce);
            cmdQueryForIndividual.CommandType = CommandType.Text;

            cmdQueryForIndividual.Parameters.AddWithValue("@LastName", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@FirstName", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@HouseholdRole", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@MembershipID", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@MembershipStatus", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@SSN", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@IndividualID", strTextSearched);
            cmdQueryForIndividual.Parameters.AddWithValue("@LagacyIndividualID", strTextSearched);

            DataTable dtIndividual = new DataTable();
            SqlDataAdapter daIndividual = new SqlDataAdapter(cmdQueryForIndividual);
            daIndividual.Fill(dtIndividual);

            gvIndividualSearched.DataSource = dtIndividual;

            //String strTextSearched = txtSearch.Text.Trim();

            //String strSqlSearchCase = "select [dbo].[tbl_case].[Case_Name], [dbo].[tbl_case].[Contact_ID], [dbo].[tbl_case].[Case_status], " +
            //                          "[dbo].[tbl_case].[CreateDate], [dbo].[tbl_case].[CreateStaff], " +
            //                          "[dbo].[tbl_case].[ModifiDate], [dbo].[tbl_case].[ModifiStaff], " +
            //                          "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Form_File_Name], [dbo].[tbl_case].[NPF_Receiv_Date], " +
            //                          "[dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Form_File_Name], [dbo].[tbl_case].[IB_Receiv_Date], " +
            //                          "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Form_File_Name], [dbo].[tbl_case].[POP_Receiv_Date], " +
            //                          "[dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Form_File_Name], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
            //                          "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Form_File_Name], [dbo].[tbl_case].[Unknown_Receiv_Date], " +
            //                          "[dbo].[tbl_case].[AddBill_Form], [dbo].[tbl_case].[AddBill_Receiv_Date] " +
            //                          "from  [dbo].[tbl_case] " +
            //                          "where [dbo].[tbl_case].[ID] like '%' + @Id + '%' or " +
            //                          "[dbo].[tbl_case].[Case_Name] like '%' + @CaseName + '%' or " +
            //                          "[dbo].[tbl_case].[Contact_ID] like '%' + @ContactId + '%' or " +
            //                          "[dbp].[tbl_case].[Log_ID] like '%' + @LogID + '%'";

            //SqlCommand cmdQueryForCase = new SqlCommand(strSqlSearchCase, connRN);
            //cmdQueryForCase.CommandType = CommandType.Text;

        }

        private void gvIndividualSearched_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //tbCMMManager.TabPages.Contains(tbpgCreateCase)
            if (tbCMMManager.TabPages.Contains(tbpgIndividual))
            {
                MessageBox.Show("Individual Page is open. Close Individual page first.", "Alert");
                return;
            }

            //int nRowSelected = gvIndividual.CurrentCell.RowIndex;
            int nRowSelected =  e.RowIndex;
            
            //IndividualSearched.strID = gvIndividualSearched["ID", nRowSelected].Value.ToString();
            //IndividualSearched.strAccountID = gvIndividualSearched["AccountID", nRowSelected].Value.ToString();
            //IndividualSearched.strLastName = gvIndividualSearched["LASTNAME", nRowSelected].Value.ToString();
            //IndividualSearched.strFirstName = gvIndividualSearched["FIRSTNAME", nRowSelected].Value.ToString();
            //IndividualSearched.strSalutation = gvIndividualSearched["SALUTATION", nRowSelected].Value.ToString();

            IndividualSearched.strIndividualID = gvIndividualSearched["Individual No.", nRowSelected]?.Value?.ToString();

            String IndividualName = gvIndividualSearched["Name", nRowSelected]?.Value?.ToString().Trim();
            IndividualSearched.strLastName = IndividualName.Substring(0, IndividualName.IndexOf(','));
            IndividualSearched.strFirstName = IndividualName.Substring(IndividualName.IndexOf(',') + 2);
            if (IndividualSearched.strFirstName.IndexOf(' ') > 0) IndividualSearched.strFirstName = IndividualSearched.strFirstName.Substring(0, IndividualSearched.strFirstName.IndexOf(' '));
            String TempIndividualName = IndividualName.Substring(IndividualName.IndexOf(' ') + 1);
            IndividualSearched.strMiddleName = String.Empty;
            if (TempIndividualName.IndexOf(' ') > 0)
            {
                IndividualSearched.strMiddleName = TempIndividualName.Substring(TempIndividualName.IndexOf(' ') + 1);
            }

            IndividualSearched.strMembershipID = gvIndividualSearched["Membership", nRowSelected]?.Value?.ToString();
            IndividualSearched.strLegacyIndividualID = gvIndividualSearched["CRM No.", nRowSelected]?.Value?.ToString();

            switch (gvIndividualSearched["Membership Status", nRowSelected].Value.ToString())
            {
                case "Pending":
                    IndividualSearched.membershipStatus = MembershipStatus.Pending;
                    break;
                case "Applied":
                    IndividualSearched.membershipStatus = MembershipStatus.Applied;
                    break;
                case "Active":
                    IndividualSearched.membershipStatus = MembershipStatus.Active;
                    break;
                case "Past Due":
                    IndividualSearched.membershipStatus = MembershipStatus.PastDue;
                    break;
                case "Inactive":
                    IndividualSearched.membershipStatus = MembershipStatus.Inactive;
                    break;
                case "Cancelled Req.":
                    IndividualSearched.membershipStatus = MembershipStatus.CancelledReq;
                    break;
                case "Cancelled by Member":
                    IndividualSearched.membershipStatus = MembershipStatus.CancelledByMember;
                    break;
                case "Terminated by CMM":
                    IndividualSearched.membershipStatus = MembershipStatus.TerminatedByCMM;
                    break;
                case "Hold":
                    IndividualSearched.membershipStatus = MembershipStatus.Hold;
                    break;
                case "Incomplete":
                    IndividualSearched.membershipStatus = MembershipStatus.Incomplete;
                    break;
                default:
                    break;
            }

            if (gvIndividualSearched["Membership Start Date", nRowSelected]?.Value?.ToString() != String.Empty)
            {
                IndividualSearched.dtMembershipIndStartDate = DateTime.Parse(gvIndividualSearched["Membership Start Date", nRowSelected]?.Value?.ToString());
            }

            if (gvIndividualSearched["Membership Cancel Date", nRowSelected]?.Value?.ToString() != String.Empty)
            {
                IndividualSearched.dtMembershipCancelledDate = DateTime.Parse(gvIndividualSearched["Membership Cancel Date", nRowSelected]?.Value?.ToString());
            }

            IndividualSearched.dtBirthDate = DateTime.Parse(gvIndividualSearched["BirthDate", nRowSelected]?.Value?.ToString());

            switch(gvIndividualSearched["Gender", nRowSelected]?.Value?.ToString())
            {
                case "Male":
                    IndividualSearched.IndividualGender = Gender.Male;
                    break;
                case "Female":
                    IndividualSearched.IndividualGender = Gender.Female;
                    break;
            }


            switch(gvIndividualSearched["House Type", nRowSelected]?.Value?.ToString())
            {
                case "Head of Household":
                    IndividualSearched.IndividualHouseholdRole = HouseholdRole.HeadOfHousehold;
                    break;
                case "Spouse":
                    IndividualSearched.IndividualHouseholdRole = HouseholdRole.Spouse;
                    break;
                case "Child":
                    IndividualSearched.IndividualHouseholdRole = HouseholdRole.Child;
                    break;
            }

            switch(gvIndividualSearched["Program Name", nRowSelected]?.Value?.ToString())
            {
                case "Gold Plus":
                    IndividualSearched.IndividualPlan = Plan.GoldPlus;
                    break;
                case "Gold":
                    IndividualSearched.IndividualPlan = Plan.Gold;
                    break;
                case "Silver":
                    IndividualSearched.IndividualPlan = Plan.Silver;
                    break;
                case "Bronze":
                    IndividualSearched.IndividualPlan = Plan.Bronze;
                    break;
                case "Gold Medi-I":
                    IndividualSearched.IndividualPlan = Plan.GoldMedi_I;
                    break;
                case "Gold Medi-II":
                    IndividualSearched.IndividualPlan = Plan.GoldMedi_II;
                    break;
            }


            //IndividualSearched.strShippingStreetAddress = gvIndividualSearched["ShippingStreet", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strShippingCity = gvIndividualSearched["ShippingCity", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strShippingState = gvIndividualSearched["ShippingState", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strShippingZip = gvIndividualSearched["ShippingPostalCode", nRowSelected]?.Value?.ToString();

            IndividualSearched.strShippingStreetAddress = gvIndividualSearched["MailingStreet", nRowSelected]?.Value?.ToString();
            IndividualSearched.strShippingCity = gvIndividualSearched["MailingCity", nRowSelected]?.Value?.ToString();
            IndividualSearched.strShippingState = gvIndividualSearched["MailingState", nRowSelected]?.Value?.ToString();
            IndividualSearched.strShippingZip = gvIndividualSearched["MailingPostalCode", nRowSelected]?.Value?.ToString();

            //IndividualSearched.strBillingStreetAddress = gvIndividualSearched["BillingStreet", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strBillingCity = gvIndividualSearched["BillingCity", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strBillingState = gvIndividualSearched["BillingState", nRowSelected]?.Value?.ToString();
            //IndividualSearched.strBillingZip = gvIndividualSearched["BillingPostalCode", nRowSelected]?.Value?.ToString();

            IndividualSearched.strBillingStreetAddress = gvIndividualSearched["OtherStreet", nRowSelected]?.Value?.ToString();
            IndividualSearched.strBillingCity = gvIndividualSearched["OtherCity", nRowSelected]?.Value?.ToString();
            IndividualSearched.strBillingState = gvIndividualSearched["OtherState", nRowSelected]?.Value?.ToString();
            IndividualSearched.strBillingZip = gvIndividualSearched["OtherPostalCode", nRowSelected]?.Value?.ToString();


            IndividualSearched.strChurch = gvIndividualSearched["Church Name", nRowSelected]?.Value?.ToString();
            IndividualSearched.strEmail = gvIndividualSearched["Email", nRowSelected]?.Value?.ToString();

            //IndividualSearched.dtBirthDate = DateTime.Parse(gvIndividualSearched["BIRTHDATE", nRowSelected].Value.ToString());
            //if (gvIndividualSearched["cmm_GENDER__C", nRowSelected].Value.ToString() == "Male") IndividualSearched.IndividualGender = Gender.Male;
            //else if (gvIndividualSearched["cmm_GENDER__C", nRowSelected].Value.ToString() == "Female") IndividualSearched.IndividualGender = Gender.Female;
            //if (gvIndividualSearched["HOUSEHOLD_ROLE__C", nRowSelected].Value.ToString() == "Head of Household")
            //    IndividualSearched.IndividualHouseholdRole = HouseholdRole.HeadOfHousehold;
            //else if (gvIndividualSearched["HOUSEHOLD_ROLE__C", nRowSelected].Value.ToString() == "Spouse") IndividualSearched.IndividualHouseholdRole = HouseholdRole.Spouse;
            //else if (gvIndividualSearched["HOUSEHOLD_ROLE__C", nRowSelected].Value.ToString() == "Child") IndividualSearched.IndividualHouseholdRole = HouseholdRole.Child;

            //IndividualSearched.strSSN = gvIndividualSearched["SOCIAL_SECURITY_NUMBER__C", nRowSelected].Value.ToString();

            //IndividualSearched.strBillingStreetAddress = gvIndividualSearched["BillingStreet", nRowSelected].Value.ToString();
            //IndividualSearched.strBillingCity = gvIndividualSearched["BillingCity", nRowSelected].Value.ToString();
            //IndividualSearched.strBillingState = gvIndividualSearched["BillingState", nRowSelected].Value.ToString();
            //IndividualSearched.strBillingZip = gvIndividualSearched["BillingPostalCode", nRowSelected].Value.ToString();
            //IndividualSearched.strShippingStreetAddress = gvIndividualSearched["ShippingStreet", nRowSelected].Value.ToString();
            //IndividualSearched.strShippingCity = gvIndividualSearched["ShippingCity", nRowSelected].Value.ToString();
            //IndividualSearched.strShippingState = gvIndividualSearched["ShippingState", nRowSelected].Value.ToString();
            //IndividualSearched.strShippingZip = gvIndividualSearched["ShippingPostalCode", nRowSelected].Value.ToString();

            //IndividualSearched.strEmail = gvIndividualSearched["Email", nRowSelected].Value.ToString();

            //if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Gold Medi-I") IndividualSearched.IndividualPlan = Plan.GoldMedi_I;
            //else if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Gold Medi-II") IndividualSearched.IndividualPlan = Plan.GoldMedi_II;
            //else if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Gold Plus") IndividualSearched.IndividualPlan = Plan.GoldPlus;
            //else if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Gold") IndividualSearched.IndividualPlan = Plan.Gold;
            //else if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Silver") IndividualSearched.IndividualPlan = Plan.Silver;
            //else if (gvIndividualSearched["ProgramName", nRowSelected].Value.ToString() == "Bronze") IndividualSearched.IndividualPlan = Plan.Bronze;

            //IndividualSearched.dtMembershipIndStartDate = DateTime.Parse(gvIndividualSearched["MembershipStartDate", nRowSelected].Value.ToString());
            //if (gvIndividualSearched["MembershipCancelDate", nRowSelected].Value.ToString() != String.Empty)
            //{
            //    IndividualSearched.dtMembershipCancelledDate = DateTime.Parse(gvIndividualSearched["MembershipCancelDate", nRowSelected].Value.ToString());
            //}
            //else IndividualSearched.dtMembershipCancelledDate = null;

            //IndividualSearched.strChurch = gvIndividualSearched["ChurchName", nRowSelected].Value.ToString();



            //DialogResult = DialogResult.OK;
            //return;

            txtMembershipID.Text = IndividualSearched.strMembershipID;
            txtIndividualID.Text = IndividualSearched.strIndividualID;
            txtCRM_ID.Text = IndividualSearched.strLegacyIndividualID;

            // 10/17/18 begin retrieving individual data here - such as preferred language, preferred communication method

            txtFirstName.Text = IndividualSearched.strFirstName;
            txtMiddleName.Text = IndividualSearched.strMiddleName;
            txtLastName.Text = IndividualSearched.strLastName;
            dtpBirthDate.Text = IndividualSearched.dtBirthDate.Value.ToString("MM/dd/yyyy");

            if (IndividualSearched.IndividualGender == Gender.Male) cbGender.SelectedIndex = 0;
            else if (IndividualSearched.IndividualGender == Gender.Female) cbGender.SelectedIndex = 1;
            //txtIndividualSSN.Text = IndividualSearched.strSSN;

            txtStreetAddress1.Text = IndividualSearched.strShippingStreetAddress;
            txtZip1.Text = IndividualSearched.strShippingZip;
            txtCity1.Text = IndividualSearched.strShippingCity;
            txtState1.Text = IndividualSearched.strShippingState;

            txtStreetAddress2.Text = IndividualSearched.strBillingStreetAddress;
            txtZip2.Text = IndividualSearched.strBillingZip;
            txtCity2.Text = IndividualSearched.strBillingCity;
            txtState2.Text = IndividualSearched.strBillingState;
            txtEmail.Text = IndividualSearched.strEmail;

            List<String> lstCommunicationMethod = new List<String>();

            String strSqlQueryForCommunicationMethods = "select [dbo].[CommunicationMethod].[CommunicationMethod] from [dbo].[CommunicationMethod]";

            SqlCommand cmdQueryForCommunicationMethod = new SqlCommand(strSqlQueryForCommunicationMethods, connSalesforce);
            cmdQueryForCommunicationMethod.CommandType = CommandType.Text;

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            SqlDataReader rdrCommunicationMethods = cmdQueryForCommunicationMethod.ExecuteReader();

            if (rdrCommunicationMethods.HasRows)
            {
                while(rdrCommunicationMethods.Read())
                {
                    lstCommunicationMethod.Add(rdrCommunicationMethods.GetString(0));
                }
            }

            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            cbPreferredCommunication.Items.Clear();

            foreach (String commMethod in lstCommunicationMethod)
            {
                cbPreferredCommunication.Items.Add(commMethod);
            }

            List<String> lstReimbursementMethod = new List<string>();

            String strSqlQueryForReimbursementMethod = "select [dbo].[ReimbursementMethod].[ReimbursementMethodValue] from [dbo].[ReimbursementMethod]";

            SqlCommand cmdQueryForReimbursementMethod = new SqlCommand(strSqlQueryForReimbursementMethod, connSalesforce);
            cmdQueryForReimbursementMethod.CommandType = CommandType.Text;

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            SqlDataReader rdrReimbursementMethod = cmdQueryForReimbursementMethod.ExecuteReader();

            if (rdrReimbursementMethod.HasRows)
            {
                while (rdrReimbursementMethod.Read())
                {
                    lstReimbursementMethod.Add(rdrReimbursementMethod.GetString(0));
                }
            }
            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            cbPaymentMethod.Items.Clear();

            foreach (String reimbursementMethod in lstReimbursementMethod)
            {
                cbPaymentMethod.Items.Add(reimbursementMethod);
            }

            String strSqlQueryForIndividualInfo = "select [dbo].[contact].[PreferredLanguage], [dbo].[contact].[PreferredCommunicationMethod], " +
                                                  "[dbo].[contact].[Phone], [dbo].[contact].[HomePhone], " +
                                                  "[dbo].[contact].[PowerOfAttorney], [dbo].[contact].[Relationship], " +
                                                  "[dbo].[contact].[ReimbursementMethod], " +
                                                  "[dbo].[contact].[Social_Security_Number__c] " +
                                                  "from [dbo].[contact] " +
                                                  "where [dbo].[contact].[Individual_ID__c] = @IndividualId";

            SqlCommand cmdQueryForIndividualInfo = new SqlCommand(strSqlQueryForIndividualInfo, connSalesforce);
            cmdQueryForIndividualInfo.CommandType = CommandType.Text;

            cmdQueryForIndividualInfo.Parameters.AddWithValue("@IndividualId", IndividualSearched.strIndividualID);

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            SqlDataReader rdrIndividualInfo = cmdQueryForIndividualInfo.ExecuteReader();
            if (rdrIndividualInfo.HasRows)
            {
                if (rdrIndividualInfo.Read())
                {
                    if (!rdrIndividualInfo.IsDBNull(0))
                    {
                        if (rdrIndividualInfo.GetInt16(0) == 0)  // Preferred language is Korean
                        {
                            rbKorean.Checked = true;
                        }
                        else if (rdrIndividualInfo.GetInt16(0) == 1) // Preferred language is English
                        {
                            rbEnglish.Checked = true;
                        }
                    }

                    if (!rdrIndividualInfo.IsDBNull(1))
                    {
                        cbPreferredCommunication.SelectedIndex = rdrIndividualInfo.GetInt16(1);
                    }

                    if (!rdrIndividualInfo.IsDBNull(2)) txtCellPhone1.Text = rdrIndividualInfo.GetString(2).Trim();
                    if (!rdrIndividualInfo.IsDBNull(3)) txtBusinessPhone.Text = rdrIndividualInfo.GetString(3).Trim();

                    if (!rdrIndividualInfo.IsDBNull(4)) txtPowerOfAttorney.Text = rdrIndividualInfo.GetString(4).Trim();
                    if (!rdrIndividualInfo.IsDBNull(5)) txtRelationship.Text = rdrIndividualInfo.GetString(5).Trim();

                    if (!rdrIndividualInfo.IsDBNull(6)) cbPaymentMethod.SelectedIndex = rdrIndividualInfo.GetInt16(6);

                    if (!rdrIndividualInfo.IsDBNull(7))
                    {
                        txtIndividualSSN.Text = rdrIndividualInfo.GetString(7).Trim();
                        IndividualSearched.strSSN = txtIndividualSSN.Text;
                    }
                }
            }

            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            txtIndChurchName.Text = IndividualSearched.strChurch;

            var srcChurch = new AutoCompleteStringCollection();

            foreach (ChurchInfo info in lstChurchInfo)
            {
                srcChurch.Add(info.Name);
            }

            txtIndChurchName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtIndChurchName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtIndChurchName.AutoCompleteCustomSource = srcChurch;      

            txtProgram.Text = IndividualSearched.IndividualPlan.ToString();

            switch (IndividualSearched.IndividualPlan)
            {
                case Plan.GoldPlus:
                    txtMemberProgram.Text = "Gold Plus";
                    break;
                case Plan.Gold:
                    txtMemberProgram.Text = "Gold";
                    break;
                case Plan.Silver:
                    txtMemberProgram.Text = "Silver";
                    break;
                case Plan.Bronze:
                    txtMemberProgram.Text = "Bronze";
                    break;
                case Plan.GoldMedi_I:
                    txtMemberProgram.Text = "Gold Medi-I";
                    break;
                case Plan.GoldMedi_II:
                    txtMemberProgram.Text = "Gold Medi-II";
                    break;
            }


            txtMembershipStartDate.Text = IndividualSearched.dtMembershipIndStartDate.Value.ToString("MM/dd/yyyy");
            if (IndividualSearched.dtMembershipCancelledDate != null)
            {
                txtMembershipCancelledDate.Text = IndividualSearched.dtMembershipCancelledDate.Value.ToString("MM/dd/yyyy");
            }
            else txtMembershipCancelledDate.Text = String.Empty;
            txtIndMemberShipStatus.Text = IndividualSearched.membershipStatus.ToString();

            IndividualIdIndividualPage = txtIndividualID.Text.Trim();

            String strSqlQueryForCaseInfo = "select distinct([dbo].[tbl_medbill].[Case_Id]), [dbo].[tbl_medbill].[Contact_Id], [dbo].[tbl_medbill].[BillStatus] " +
                                            "from [dbo].[tbl_medbill] " +
                                            "where [dbo].[tbl_medbill].[Contact_Id] = @IndividualId and " +
                                            "[dbo].[tbl_medbill].[IsDeleted] = 0 and " +
                                            "([dbo].[tbl_medbill].[BillStatus] = @BillStatusCode0 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode1 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode2 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode3 or " +
                                            "[dbo].[tbl_medbill].[BillStatus] = @BillStatusCode4)";

            SqlCommand cmdQueryForCaseInfo = new SqlCommand(strSqlQueryForCaseInfo, connRN);
            cmdQueryForCaseInfo.CommandType = CommandType.Text;

            cmdQueryForCaseInfo.Parameters.AddWithValue("@IndividualId", IndividualIdIndividualPage);
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode0", 0);     // Pending
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode1", 1);     // CMM Pending Payment  
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode2", 2);     // Closed
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode3", 3);     // Ineligible
            cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode4", 4);     // Partially Ineligible
                                                                                    //cmdQueryForCaseInfo.Parameters.AddWithValue("@BillStatusCode5", 5);    

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCaseInfo = cmdQueryForCaseInfo.ExecuteReader();

            lstCaseInfo.Clear();
            if (rdrCaseInfo.HasRows)
            {
                while (rdrCaseInfo.Read())
                {
                    lstCaseInfo.Add(new CaseInfo { CaseName = rdrCaseInfo.GetString(0), IndividualId = rdrCaseInfo.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            gvProcessingCaseNo.Rows.Clear();
            //if (lstCaseInfo.Count > 0)
            //{



            String strSqlQueryForCasesForIndividualID = "select distinct([dbo].[tbl_case].[Case_Name]), [dbo].[tbl_case].[CreateDate], [dbo].[tbl_CreateStaff].[Staff_Name], " +
                                                        "[dbo].[tbl_case].[NPF_Form], [dbo].[tbl_case].[NPF_Receiv_Date], [dbo].[tbl_case].[IB_Form], [dbo].[tbl_case].[IB_Receiv_Date], " +
                                                        "[dbo].[tbl_case].[POP_Form], [dbo].[tbl_case].[POP_Receiv_Date], [dbo].[tbl_case].[MedRec_Form], [dbo].[tbl_case].[MedRec_Receiv_Date], " +
                                                        "[dbo].[tbl_case].[Unknown_Form], [dbo].[tbl_case].[Unknown_Receiv_Date] " +
                                                        "from [dbo].[tbl_case] " +
                                                        "inner join [dbo].[tbl_medbill] on [dbo].[tbl_case].[Case_Name] = [dbo].[tbl_medbill].[Case_Id] " +
                                                        "inner join [dbo].[tbl_CreateStaff] on [dbo].[tbl_case].[CreateStaff] = [dbo].[tbl_CreateStaff].[CreateStaff_Id] " +
                                                        "where [dbo].[tbl_case].[Contact_ID] = @IndividualID and " +
                                                        "[dbo].[tbl_case].[IsDeleted] = 0 and " +
                                                        "([dbo].[tbl_medbill].[BillStatus] = 0 or [dbo].[tbl_medbill].[BillStatus] = 1 or [dbo].[tbl_medbill].[BillStatus] = 4) " +
                                                        "order by [dbo].[tbl_case].[Case_Name]";


            SqlCommand cmdQueryForCasesIndividualPage = new SqlCommand(strSqlQueryForCasesForIndividualID, connRN);
            cmdQueryForCasesIndividualPage.CommandType = CommandType.Text;
            cmdQueryForCasesIndividualPage.Parameters.AddWithValue("@IndividualID", IndividualIdIndividualPage);
            //cmdQueryForCasesIndividualPage.Parameters.AddWithValue("@IndividualID", lstCaseInfo[0].IndividualId);

            cmdQueryForCasesIndividualPage.Notification = null;

            SqlDependency dependencyCaseForIndividual = new SqlDependency(cmdQueryForCasesIndividualPage);
            dependencyCaseForIndividual.OnChange += new OnChangeEventHandler(OnCaseForIndividualChange);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCasesForIndividual = cmdQueryForCasesIndividualPage.ExecuteReader();

            //gvProcessingCaseNo.Rows.Clear();
            if (rdrCasesForIndividual.HasRows)
            {
                while (rdrCasesForIndividual.Read())
                {
                    for (int i = 0; i < lstCaseInfo.Count; i++)
                    {
                        if ((!rdrCasesForIndividual.IsDBNull(0)) &&
                            (rdrCasesForIndividual.GetString(0) == lstCaseInfo[i].CaseName))
                        {

                            DataGridViewRow row = new DataGridViewRow();

                            row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(0)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetString(0) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(1) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(2)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetString(2) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(3)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(3) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(4)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(4) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(5)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(5) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(6)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(6) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(7)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(7) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = false });

                            if (!rdrCasesForIndividual.IsDBNull(8)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(8) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(9)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(9) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(10)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(10) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(11)) row.Cells.Add(new DataGridViewCheckBoxCell { Value = rdrCasesForIndividual.GetBoolean(11) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            if (!rdrCasesForIndividual.IsDBNull(12)) row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrCasesForIndividual.GetDateTime(12) });
                            else row.Cells.Add(new DataGridViewTextBoxCell { Value = String.Empty });

                            gvProcessingCaseNo.Rows.Add(row);
                        }
                    }
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();
            //}

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Case History Page

            strIndividualId = IndividualSearched.strIndividualID.Trim();

            txtCaseHistoryIndividualID.Text = strIndividualId;

            txtCaseHistoryIndividualName.Text = txtLastName.Text + ", " + txtFirstName.Text + " " + txtMiddleName.Text;

            String strSqlQueryForCreateStaff = "select dbo.tbl_CreateStaff.CreateStaff_Id, dbo.tbl_CreateStaff.Staff_Name from dbo.tbl_CreateStaff";

            SqlCommand cmdQueryForCreateStaff = new SqlCommand(strSqlQueryForCreateStaff, connRN);
            cmdQueryForCreateStaff.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrCreateStaff = cmdQueryForCreateStaff.ExecuteReader();

            lstCreateStaff.Clear();
            if (rdrCreateStaff.HasRows)
            {
                while (rdrCreateStaff.Read())
                {
                    lstCreateStaff.Add(new StaffInfo { StaffId = rdrCreateStaff.GetInt16(0), StaffName = rdrCreateStaff.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            String strSqlQueryForModifiStaff = "select dbo.tbl_ModifiStaff.ModifiStaff_Id, dbo.tbl_ModifiStaff.Staff_Name from dbo.tbl_ModifiStaff";

            SqlCommand cmdQueryForModifiStaff = new SqlCommand(strSqlQueryForModifiStaff, connRN);
            cmdQueryForModifiStaff.CommandType = CommandType.Text;

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader rdrModifiStaff = cmdQueryForModifiStaff.ExecuteReader();

            lstModifiStaff.Clear();
            if (rdrModifiStaff.HasRows)
            {
                while (rdrModifiStaff.Read())
                {
                    lstModifiStaff.Add(new StaffInfo { StaffId = rdrModifiStaff.GetInt16(0), StaffName = rdrModifiStaff.GetString(1) });
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            String strSqlQueryForCases = "select dbo.tbl_case.Case_Name, dbo.tbl_case.CreateDate, dbo.tbl_case.CreateStaff, " +
                                         "dbo.tbl_case.ModifiDate, dbo.tbl_case.ModifiStaff " +
                                         "from dbo.tbl_case where individual_id = @IndividualId and " +
                                         "[dbo].[tbl_case].[IsDeleted] = 0 " +
                                         "order by [dbo].[tbl_case].[ID]";


            SqlCommand cmdQueryForCases = new SqlCommand(strSqlQueryForCases, connRN);
            cmdQueryForCases.CommandType = CommandType.Text;

            cmdQueryForCases.Parameters.AddWithValue("@IndividualId", strIndividualId);

            SqlDependency dependencyCase = new SqlDependency(cmdQueryForCases);
            dependencyCase.OnChange += new OnChangeEventHandler(OnCaseChange);


            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            SqlDataReader reader = cmdQueryForCases.ExecuteReader();
            gvCaseViewCaseHistory.Rows.Clear();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetString(0) });     // Case ID

                    // Create Date
                    if (!reader.IsDBNull(1)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(1).ToString("MM/dd/yyyy") });

                    // Create Staff
                    if (!reader.IsDBNull(2))
                    {
                        for (int i = 0; i < lstCreateStaff.Count; i++)
                        {
                            if (reader.GetInt16(2) == lstCreateStaff[i].StaffId)
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = lstCreateStaff[i].StaffName });
                        }
                    }

                    // Modifi Date
                    if (!reader.IsDBNull(3)) row.Cells.Add(new DataGridViewTextBoxCell { Value = reader.GetDateTime(3).ToString("MM/dd/yyyy") });

                    // Modifi Staff
                    if (!reader.IsDBNull(4))
                    {
                        for (int i = 0; i < lstModifiStaff.Count; i++)
                        {
                            if (reader.GetInt16(4) == lstModifiStaff[i].StaffId)
                                row.Cells.Add(new DataGridViewTextBoxCell { Value = lstModifiStaff[i].StaffName });
                        }
                    }
                    gvCaseViewCaseHistory.Rows.Add(row);
                }
            }
            if (connRN.State == ConnectionState.Open) connRN.Close();

            //tbCMMManager.TabPages.Add(tbpgIndividual);
            //tbCMMManager.TabPages.Add(tbpgCaseView);
            tbCMMManager.TabPages.Insert(2, tbpgIndividual);
            //tbCMMManager.TabPages.Insert(3, tbpgCaseView);
            tbCMMManager.SelectedIndex = 2;

            PrevTabPage = TabPage.None;
            CurrentTabPage = TabPage.DashBoard;
        }

        private void btnDeleteMedBill_Click(object sender, EventArgs e)
        {

            DialogResult dlgResult = MessageBox.Show("Are you sure to delete selected Med Bills.", "Warning", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                Boolean bError = false;
                int nRowSelected = 0;

                for (int i = 0; i < gvCasePageMedBills.Rows.Count; i++)
                {
                    DataGridViewCheckBoxCell chkMedBillCell = gvCasePageMedBills["MedBillSelected", i] as DataGridViewCheckBoxCell;

                    if ((Boolean)chkMedBillCell.Value == true)
                    {
                        nRowSelected++;
                        String MedBillToDelete = gvCasePageMedBills["MedBillNo", i].Value as String;

                        //String strSqlDeleteMedBill = "delete from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";
                        String strSqlDeleteMedBill = "update [dbo].[tbl_medbill] set [dbo].[tbl_medbill].[IsDeleted] = 1 where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";

                        SqlCommand cmdDeleteMedBill = new SqlCommand(strSqlDeleteMedBill, connRN);
                        cmdDeleteMedBill.CommandType = CommandType.Text;

                        cmdDeleteMedBill.Parameters.AddWithValue("@MedBillNo", MedBillToDelete);

                        //if (connRN.State == ConnectionState.Closed) connRN.Open();
                        if (connRN.State == ConnectionState.Open)
                        {
                            connRN.Close();
                            connRN.Open();
                        }
                        else if (connRN.State == ConnectionState.Closed) connRN.Open();
                        int nRowDeleted = cmdDeleteMedBill.ExecuteNonQuery();
                        if (connRN.State == ConnectionState.Open) connRN.Close();

                        if (nRowDeleted == 0)
                        {
                            bError = true;
                        }
                    }
                }

                if ((bError == true) && (nRowSelected > 0))
                {
                    MessageBox.Show("Some of selected Medical Bills have not deleted.", "Error");
                    return;
                }
                if ((bError == false) && (nRowSelected > 0))
                {
                    MessageBox.Show("Medical Bills have been deleted.", "Information");
                    return;
                }
            }
            else return;
        }

        private void btnCaseCreationLowerSave_Click(object sender, EventArgs e)
        {
            String CaseName = txtCaseName.Text.Trim();
            String IndividualId = txtCaseIndividualID.Text.Trim();

            String strSqlQueryForCaseName = "select [dbo].[tbl_case].[Case_Name] from [dbo].[tbl_case] " +
                                            "where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualId";

            SqlCommand cmdQueryForCaseName = new SqlCommand(strSqlQueryForCaseName, connRN);
            cmdQueryForCaseName.CommandText = strSqlQueryForCaseName;
            cmdQueryForCaseName.CommandType = CommandType.Text;

            cmdQueryForCaseName.Parameters.AddWithValue("@CaseName", CaseName);
            cmdQueryForCaseName.Parameters.AddWithValue("@IndividualId", IndividualId);

            //if (connRN.State == ConnectionState.Closed) connRN.Open();
            if (connRN.State == ConnectionState.Open)
            {
                connRN.Close();
                connRN.Open();
            }
            else if (connRN.State == ConnectionState.Closed) connRN.Open();
            Object objCaseName = cmdQueryForCaseName.ExecuteScalar();
            if (connRN.State == ConnectionState.Open) connRN.Close();

            if (objCaseName == null)
            {
                frmSaveNewCase frmSaveNewCase = new frmSaveNewCase();
                frmSaveNewCase.StartPosition = FormStartPosition.CenterParent;

                DialogResult dlgResult = frmSaveNewCase.ShowDialog();

                if (dlgResult == DialogResult.Yes)
                {

                    String strCaseId = String.Empty;
                    String strIndividualID = String.Empty;
                    String strNPFormFilePath = String.Empty;
                    String strNPFUploadDate = String.Empty;
                    String strIBFilePath = String.Empty;
                    String strIBUploadDate = String.Empty;
                    String strPopFilePath = String.Empty;
                    String strPopUploadDate = String.Empty;
                    String strMedicalRecordFilePath = String.Empty;
                    String strMedicalRecordUploadDate = String.Empty;
                    String strUnknownDocumentFilePath = String.Empty;
                    String strUnknownDocUploadDate = String.Empty;
                    String strLogID = String.Empty;

                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = String.Empty;
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    caseDetail.ModificationDate = DateTime.Today;
                    caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;

                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;

                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;

                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;

                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;

                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;

                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();
                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = false;
                    caseDetail.AddBill_Received_Date = null;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlCreateCase = "insert into tbl_case (IsDeleted, Case_Name, Contact_ID, CreateDate, ModifiDate, CreateStaff, ModifiStaff, Case_status, " +
                                               "NPF_Form, NPF_Form_File_Name, NPF_Form_Destination_File_Name, NPF_Receiv_Date, " +
                                               "IB_Form, IB_Form_File_Name, IB_Form_Destination_File_Name, IB_Receiv_Date, " +
                                               "POP_Form, POP_Form_File_Name, POP_Form_Destination_File_Name, POP_Receiv_Date, " +
                                               "MedRec_Form, MedRec_Form_File_Name, MedRec_Form_Destination_File_Name, MedRec_Receiv_Date, " +
                                               "Unknown_Form, Unknown_Form_File_Name, Unknown_Form_Destination_File_Name, Unknown_Receiv_Date, " +
                                               "Note, Log_ID, AddBill_Form, AddBill_receiv_Date, Remove_log, individual_id) " +
                                               "Values (@IsDeleted, @CaseId, @ContactId, @CreateDate, @ModifiDate, @CreateStaff, @ModifiStaff, @CaseStatus, " +
                                               "@NPF_Form, @NPF_Form_File_Name, @NPF_Form_Destination_File_Name, @NPF_Receive_Date, " +
                                               "@IB_Form, @IB_Form_File_Name, @IB_Form_Destination_File_Name, @IB_Receive_Date, " +
                                               "@POP_Form, @POP_Form_File_Name, @POP_Form_Destination_File_Name, @POP_Receive_Date, " +
                                               "@MedRecord_Form, @MedRecord_Form_File_Name, @MedRecord_Form_Destination_File_name, @MedRecord_Receive_Date, " +
                                               "@Unknown_Form, @Unknown_Form_File_Name, @Unknown_Form_Destination_File_Name, @Unknown_Receive_Date, " +
                                               "@Note, @Log_Id, @AddBill_Form, @AddBill_ReceiveDate, @Remove_Log, @Individual_Id)";

                    SqlCommand cmdInsertNewCase = new SqlCommand(strSqlCreateCase, connRN);
                    cmdInsertNewCase.CommandType = CommandType.Text;

                    cmdInsertNewCase.Parameters.AddWithValue("@IsDeleted", 0);
                    cmdInsertNewCase.Parameters.AddWithValue("@CaseId", caseDetail.CaseId);
                    cmdInsertNewCase.Parameters.AddWithValue("@ContactId", caseDetail.ContactId);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateDate", caseDetail.CreateDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateStaff", caseDetail.CreateStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@CaseStatus", caseDetail.Status);

                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", caseDetail.NPF_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", caseDetail.IB_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", caseDetail.POP_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form", caseDetail.MedicalRecord_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Note", caseDetail.Note);
                    cmdInsertNewCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdInsertNewCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", caseDetail.AddBill_Received_Date);
                    else cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", DBNull.Value);
                    if (caseDetail.Remove_Log == String.Empty) cmdInsertNewCase.Parameters.AddWithValue("@Remove_Log", DBNull.Value);
                    cmdInsertNewCase.Parameters.AddWithValue("@Individual_Id", caseDetail.Individual_Id);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nResult = cmdInsertNewCase.ExecuteNonQuery();
                    if (nResult == 1)
                    {
                        MessageBox.Show("The case has been saved.", "Information");

                        caseDetail.CaseId = txtCaseName.Text.Trim();
                        strCaseIdSelected = caseDetail.CaseId;
                        strContactIdSelected = caseDetail.ContactId;

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();
                }
                else if (dlgResult == DialogResult.Cancel)
                {
                    return;
                }

                btnNewMedBill_Case.Enabled = true;
                btnEditMedBill.Enabled = true;
                btnDeleteMedBill.Enabled = true;
                //else if (dlgResult == DialogResult.No)
                //{
                //    //tbCMMManager.TabPages.Remove(tbpgCreateCase);
                //    //tbCMMManager.SelectedIndex = 3;

                //    return;
                //}

            }
            else if (objCaseName != null)    // Edit and update case
            {
                frmSaveChangeOnCase frmDlgSaveChange = new frmSaveChangeOnCase();

                frmDlgSaveChange.StartPosition = FormStartPosition.CenterParent;
                DialogResult dlgResult = frmDlgSaveChange.ShowDialog();

                //if (frmDlgSaveChange.DialogResult == DialogResult.Yes)
                if (dlgResult == DialogResult.Yes)
                {
                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = txtCaseName.Text.Trim();
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    //caseDetail.ModificationDate = DateTime.Today;
                    //caseDetail.CreateStaff = 8;     // WonJik
                    //caseDetail.ModifyingStaff = 8;  // WonJik
                    //caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;
                    //caseDetail.NPF_ReceivedDate = DateTime.Today;
                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;
                    //caseDetail.IB_ReceivedDate = DateTime.Today;
                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;
                    //caseDetail.POP_ReceivedDate = DateTime.Today;
                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;
                    //caseDetail.MedRec_ReceivedDate = DateTime.Today;
                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;
                    //caseDetail.Unknown_ReceivedDate = DateTime.Today;
                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;
                    //caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();

                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Note = txtNoteOnCase.Text.Trim();
                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = true;
                    caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlUpdateCase = "Update [dbo].[tbl_case] set [dbo].[tbl_case].[ModifiDate] = @ModifiDate, [dbo].[tbl_case].[ModifiStaff] = @ModifiStaff, " +
                                              "[dbo].[tbl_case].[NPF_Form] = @NPF_Form, [dbo].[tbl_case].[NPF_Form_File_Name] = @NPF_Form_File_Name, " +
                                              "[dbo].[tbl_case].[NPF_Form_Destination_File_Name] = @NPF_Form_Destination_File_Name, [dbo].[tbl_case].[NPF_Receiv_Date] = @NPF_Receiv_Date, " +
                                              "[dbo].[tbl_case].[IB_Form] = @IB_Form, [dbo].[tbl_case].[IB_Form_File_Name] = @IB_Form_File_Name, " +
                                              "[dbo].[tbl_case].[IB_Form_Destination_File_Name] = @IB_Form_Destination_File_Name, [dbo].[tbl_case].[IB_Receiv_Date] = @IB_Receiv_Date, " +
                                              "[dbo].[tbl_case].[POP_Form] = @POP_Form, [dbo].[tbl_case].[POP_Form_File_Name] = @POP_Form_File_Name, " +
                                              "[dbo].[tbl_case].[POP_Form_Destination_File_Name] = @POP_Form_Destination_File_Name, [dbo].[tbl_case].[POP_Receiv_Date] = @POP_Receiv_Date, " +
                                              "[dbo].[tbl_case].[MedRec_Form] = @MedRec_Form, [dbo].[tbl_case].[MedRec_Form_File_Name] = @MedRec_Form_File_Name, " +
                                              "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name] = @MedRec_Form_Destination_File_Name, [dbo].[tbl_case].[MedRec_Receiv_Date] = @MedRec_Receiv_Date, " +
                                              "[dbo].[tbl_case].[Unknown_Form] = @Unknown_Form, [dbo].[tbl_case].[Unknown_Form_File_Name] = @Unknown_Form_File_Name, " +
                                              "[dbo].[tbl_case].[Unknown_Form_Destination_File_Name] = @Unknown_Form_Destination_File_Name, [dbo].[tbl_case].[Unknown_Receiv_Date] = @Unknown_Receiv_Date, " +
                                              "[dbo].[tbl_case].[Note] = @CaseNote, [dbo].[tbl_case].[Log_ID] = @Log_Id, [dbo].[tbl_case].[AddBill_Form] = @AddBill_Form, " +
                                              "[dbo].[tbl_case].[AddBill_Receiv_Date] = @AddBill_Receiv_Date, [dbo].[tbl_case].[Remove_Log] = @Remove_Log " +
                                              "where [dbo].[tbl_case].[Case_Name] = @Case_Id";

                    SqlCommand cmdUpdateCase = new SqlCommand(strSqlUpdateCase, connRN);
                    cmdUpdateCase.CommandType = CommandType.Text;

                    cmdUpdateCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdUpdateCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", caseDetail.NPF_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", caseDetail.IB_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", caseDetail.POP_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form", caseDetail.MedicalRecord_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@CaseNote", caseDetail.Note);
                    cmdUpdateCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdUpdateCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", caseDetail.AddBill_Received_Date);
                    else cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@Remove_Log", caseDetail.Remove_Log);

                    cmdUpdateCase.Parameters.AddWithValue("@Case_Id", caseDetail.CaseId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowAffected = cmdUpdateCase.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowAffected == 1)
                    {
                        MessageBox.Show("The change has been saved.", "Information");

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }
                    else if (nRowAffected == 0) MessageBox.Show("Update failed", "Error");
                }
                else
                {
                    return;
                }
            }
        }

        private void btnCaseCreationLowerCancel_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Do you want save the change?", "Alert", MessageBoxButtons.YesNoCancel);

            if (dlgResult == DialogResult.Yes)
            {
                String CaseName = txtCaseName.Text.Trim();
                String IndividualId = txtCaseIndividualID.Text.Trim();

                String strSqlQueryForCaseName = "select [dbo].[tbl_case].[Case_Name] from [dbo].[tbl_case] " +
                                                "where [dbo].[tbl_case].[Case_Name] = @CaseName and [dbo].[tbl_case].[Contact_ID] = @IndividualId";

                SqlCommand cmdQueryForCaseName = new SqlCommand(strSqlQueryForCaseName, connRN);
                cmdQueryForCaseName.CommandText = strSqlQueryForCaseName;
                cmdQueryForCaseName.CommandType = CommandType.Text;

                cmdQueryForCaseName.Parameters.AddWithValue("@CaseName", CaseName);
                cmdQueryForCaseName.Parameters.AddWithValue("@IndividualId", IndividualId);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                Object objCaseName = cmdQueryForCaseName.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                if (objCaseName == null)
                {
                    //frmSaveNewCase frmSaveNewCase = new frmSaveNewCase();

                    //DialogResult dlgResult = frmSaveNewCase.ShowDialog();

                    //if (dlgResult == DialogResult.Yes)
                    //{

                    String strCaseId = String.Empty;
                    String strIndividualID = String.Empty;
                    String strNPFormFilePath = String.Empty;
                    String strNPFUploadDate = String.Empty;
                    String strIBFilePath = String.Empty;
                    String strIBUploadDate = String.Empty;
                    String strPopFilePath = String.Empty;
                    String strPopUploadDate = String.Empty;
                    String strMedicalRecordFilePath = String.Empty;
                    String strMedicalRecordUploadDate = String.Empty;
                    String strUnknownDocumentFilePath = String.Empty;
                    String strUnknownDocUploadDate = String.Empty;
                    String strLogID = String.Empty;

                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = String.Empty;
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    caseDetail.ModificationDate = DateTime.Today;
                    caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;

                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;

                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;

                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;

                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;

                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;

                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();
                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = false;
                    caseDetail.AddBill_Received_Date = null;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlCreateCase = "insert into tbl_case (Case_Name, Contact_ID, CreateDate, ModifiDate, CreateStaff, ModifiStaff, Case_status, " +
                                                "NPF_Form, NPF_Form_File_Name, NPF_Form_Destination_File_Name, NPF_Receiv_Date, " +
                                                "IB_Form, IB_Form_File_Name, IB_Form_Destination_File_Name, IB_Receiv_Date, " +
                                                "POP_Form, POP_Form_File_Name, POP_Form_Destination_File_Name, POP_Receiv_Date, " +
                                                "MedRec_Form, MedRec_Form_File_Name, MedRec_Form_Destination_File_Name, MedRec_Receiv_Date, " +
                                                "Unknown_Form, Unknown_Form_File_Name, Unknown_Form_Destination_File_Name, Unknown_Receiv_Date, " +
                                                "Note, Log_ID, AddBill_Form, AddBill_receiv_Date, Remove_log, individual_id) " +
                                                "Values (@CaseId, @ContactId, @CreateDate, @ModifiDate, @CreateStaff, @ModifiStaff, @CaseStatus, " +
                                                "@NPF_Form, @NPF_Form_File_Name, @NPF_Form_Destination_File_Name, @NPF_Receive_Date, " +
                                                "@IB_Form, @IB_Form_File_Name, @IB_Form_Destination_File_Name, @IB_Receive_Date, " +
                                                "@POP_Form, @POP_Form_File_Name, @POP_Form_Destination_File_Name, @POP_Receive_Date, " +
                                                "@MedRecord_Form, @MedRecord_Form_File_Name, @MedRecord_Form_Destination_File_name, @MedRecord_Receive_Date, " +
                                                "@Unknown_Form, @Unknown_Form_File_Name, @Unknown_Form_Destination_File_Name, @Unknown_Receive_Date, " +
                                                "@Note, @Log_Id, @AddBill_Form, @AddBill_ReceiveDate, @Remove_Log, @Individual_Id)";

                    SqlCommand cmdInsertNewCase = new SqlCommand(strSqlCreateCase, connRN);
                    cmdInsertNewCase.CommandType = CommandType.Text;

                    cmdInsertNewCase.Parameters.AddWithValue("@CaseId", caseDetail.CaseId);
                    cmdInsertNewCase.Parameters.AddWithValue("@ContactId", caseDetail.ContactId);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateDate", caseDetail.CreateDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdInsertNewCase.Parameters.AddWithValue("@CreateStaff", caseDetail.CreateStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdInsertNewCase.Parameters.AddWithValue("@CaseStatus", caseDetail.Status);

                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", caseDetail.NPF_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@NPF_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", caseDetail.IB_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@IB_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", caseDetail.POP_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form", caseDetail.MedicalRecord_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@MedRecord_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdInsertNewCase.Parameters.AddWithValue("@Unknown_Receive_Date", DBNull.Value);

                    cmdInsertNewCase.Parameters.AddWithValue("@Note", caseDetail.Note);
                    cmdInsertNewCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdInsertNewCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", caseDetail.AddBill_Received_Date);
                    else cmdInsertNewCase.Parameters.AddWithValue("@AddBill_ReceiveDate", DBNull.Value);
                    if (caseDetail.Remove_Log == String.Empty) cmdInsertNewCase.Parameters.AddWithValue("@Remove_Log", DBNull.Value);
                    cmdInsertNewCase.Parameters.AddWithValue("@Individual_Id", caseDetail.Individual_Id);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nResult = cmdInsertNewCase.ExecuteNonQuery();
                    if (nResult == 1)
                    {
                        MessageBox.Show("The case has been saved.", "Information");

                        caseDetail.CaseId = txtCaseName.Text.Trim();
                        strCaseIdSelected = caseDetail.CaseId;
                        strContactIdSelected = caseDetail.ContactId;

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }
                    if (connRN.State == ConnectionState.Open) connRN.Close();
                }
                else if (objCaseName != null)    // Edit and update case
                {
                    //frmSaveChangeOnCase frmDlgSaveChange = new frmSaveChangeOnCase();

                    //DialogResult dlgResult = frmDlgSaveChange.ShowDialog();

                    ////if (frmDlgSaveChange.DialogResult == DialogResult.Yes)
                    //if (dlgResult == DialogResult.Yes)
                    //{
                    CasedInfoDetailed caseDetail = new CasedInfoDetailed();

                    caseDetail.CaseId = txtCaseName.Text.Trim();
                    caseDetail.ContactId = String.Empty;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.CreateDate = DateTime.Today;
                    //caseDetail.ModificationDate = DateTime.Today;
                    //caseDetail.CreateStaff = 8;     // WonJik
                    //caseDetail.ModifyingStaff = 8;  // WonJik
                    //caseDetail.CreateStaff = nLoggedUserId;
                    caseDetail.ModifyingStaff = nLoggedUserId;
                    caseDetail.Status = false;
                    caseDetail.Individual_Id = String.Empty;
                    caseDetail.NPF_Form = 0;
                    caseDetail.NPF_Form_File_Name = String.Empty;
                    caseDetail.NPF_Form_Destination_File_Name = String.Empty;
                    //caseDetail.NPF_ReceivedDate = DateTime.Today;
                    caseDetail.IB_Form = 0;
                    caseDetail.IB_Form_File_Name = String.Empty;
                    caseDetail.IB_Form_Destination_File_Name = String.Empty;
                    //caseDetail.IB_ReceivedDate = DateTime.Today;
                    caseDetail.POP_Form = 0;
                    caseDetail.POP_Form_File_Name = String.Empty;
                    caseDetail.POP_Form_Destionation_File_Name = String.Empty;
                    //caseDetail.POP_ReceivedDate = DateTime.Today;
                    caseDetail.MedicalRecord_Form = 0;
                    caseDetail.MedRec_Form_File_Name = String.Empty;
                    caseDetail.MedRec_Form_Destination_File_Name = String.Empty;
                    //caseDetail.MedRec_ReceivedDate = DateTime.Today;
                    caseDetail.Unknown_Form = 0;
                    caseDetail.Unknown_Form_File_Name = String.Empty;
                    caseDetail.Unknown_Form_Destination_File_Name = String.Empty;
                    //caseDetail.Unknown_ReceivedDate = DateTime.Today;
                    caseDetail.Note = String.Empty;
                    caseDetail.Log_Id = String.Empty;
                    caseDetail.AddBill_Form = false;
                    //caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    if (txtCaseName.Text.Trim() != String.Empty) caseDetail.CaseId = txtCaseName.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.ContactId = txtCaseIndividualID.Text.Trim();
                    if (txtCaseIndividualID.Text.Trim() != String.Empty) caseDetail.Individual_Id = txtCaseIndividualID.Text.Trim();

                    if (chkNPF_CaseCreationPage.Checked)
                    {
                        caseDetail.NPF_Form = 1;
                        if (txtNPFFormFilePath.Text.Trim() != String.Empty) caseDetail.NPF_Form_File_Name = txtNPFFormFilePath.Text.Trim();
                        if (txtNPFUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtNPFUploadDate.Text.Trim(), out result)) caseDetail.NPF_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.NPF_Form_Destination_File_Name = strNPFormFilePathDestination;
                    }
                    if (chkIB_CaseCreationPage.Checked)
                    {
                        caseDetail.IB_Form = 1;
                        if (txtIBFilePath.Text.Trim() != String.Empty) caseDetail.IB_Form_File_Name = txtIBFilePath.Text.Trim();
                        if (txtIBUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtIBUploadDate.Text.Trim(), out result)) caseDetail.IB_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.IB_Form_Destination_File_Name = strIBFilePathDestination;
                    }
                    if (chkPoP_CaseCreationPage.Checked)
                    {
                        caseDetail.POP_Form = 1;
                        if (txtPopFilePath.Text.Trim() != String.Empty) caseDetail.POP_Form_File_Name = txtPopFilePath.Text.Trim();
                        if (txtPoPUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtPoPUploadDate.Text.Trim(), out result)) caseDetail.POP_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.POP_Form_Destionation_File_Name = strPopFilePathDestination;
                    }
                    if (chkMedicalRecordCaseCreationPage.Checked)
                    {
                        caseDetail.MedicalRecord_Form = 1;
                        if (txtMedicalRecordFilePath.Text.Trim() != String.Empty) caseDetail.MedRec_Form_File_Name = txtMedicalRecordFilePath.Text.Trim();
                        if (txtMRUploadDate.Text.Trim() != String.Empty)
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtMRUploadDate.Text.Trim(), out result)) caseDetail.MedRec_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.MedRec_Form_Destination_File_Name = strMedRecordFilePathDestination;
                    }
                    if (chkOtherDocCaseCreationPage.Checked)
                    {
                        caseDetail.Unknown_Form = 1;
                        if (txtOtherDocumentFilePath.Text.Trim() != String.Empty) caseDetail.Unknown_Form_File_Name = txtOtherDocumentFilePath.Text.Trim();
                        if (txtOtherDocUploadDate.Text.Trim() != String.Empty)      //caseDetail.Unknown_ReceivedDate = DateTime.Parse(txtOtherDocUploadDate.Text.Trim());
                        {
                            DateTime result;
                            if (DateTime.TryParse(txtOtherDocUploadDate.Text.Trim(), out result)) caseDetail.Unknown_ReceivedDate = result;
                            else MessageBox.Show("Invalid DateTime value", "Error");
                        }
                        caseDetail.Unknown_Form_Destination_File_Name = strUnknownDocFilePathDestination;
                    }

                    caseDetail.Note = txtNoteOnCase.Text.Trim();
                    caseDetail.Log_Id = "Log: " + txtCaseName.Text;
                    caseDetail.AddBill_Form = true;
                    caseDetail.AddBill_Received_Date = DateTime.Today;
                    caseDetail.Remove_Log = String.Empty;

                    String strSqlUpdateCase = "Update [dbo].[tbl_case] set [dbo].[tbl_case].[ModifiDate] = @ModifiDate, [dbo].[tbl_case].[ModifiStaff] = @ModifiStaff, " +
                                                "[dbo].[tbl_case].[NPF_Form] = @NPF_Form, [dbo].[tbl_case].[NPF_Form_File_Name] = @NPF_Form_File_Name, " +
                                                "[dbo].[tbl_case].[NPF_Form_Destination_File_Name] = @NPF_Form_Destination_File_Name, [dbo].[tbl_case].[NPF_Receiv_Date] = @NPF_Receiv_Date, " +
                                                "[dbo].[tbl_case].[IB_Form] = @IB_Form, [dbo].[tbl_case].[IB_Form_File_Name] = @IB_Form_File_Name, " +
                                                "[dbo].[tbl_case].[IB_Form_Destination_File_Name] = @IB_Form_Destination_File_Name, [dbo].[tbl_case].[IB_Receiv_Date] = @IB_Receiv_Date, " +
                                                "[dbo].[tbl_case].[POP_Form] = @POP_Form, [dbo].[tbl_case].[POP_Form_File_Name] = @POP_Form_File_Name, " +
                                                "[dbo].[tbl_case].[POP_Form_Destination_File_Name] = @POP_Form_Destination_File_Name, [dbo].[tbl_case].[POP_Receiv_Date] = @POP_Receiv_Date, " +
                                                "[dbo].[tbl_case].[MedRec_Form] = @MedRec_Form, [dbo].[tbl_case].[MedRec_Form_File_Name] = @MedRec_Form_File_Name, " +
                                                "[dbo].[tbl_case].[MedRec_Form_Destination_File_Name] = @MedRec_Form_Destination_File_Name, [dbo].[tbl_case].[MedRec_Receiv_Date] = @MedRec_Receiv_Date, " +
                                                "[dbo].[tbl_case].[Unknown_Form] = @Unknown_Form, [dbo].[tbl_case].[Unknown_Form_File_Name] = @Unknown_Form_File_Name, " +
                                                "[dbo].[tbl_case].[Unknown_Form_Destination_File_Name] = @Unknown_Form_Destination_File_Name, [dbo].[tbl_case].[Unknown_Receiv_Date] = @Unknown_Receiv_Date, " +
                                                "[dbo].[tbl_case].[Note] = @CaseNote, [dbo].[tbl_case].[Log_ID] = @Log_Id, [dbo].[tbl_case].[AddBill_Form] = @AddBill_Form, " +
                                                "[dbo].[tbl_case].[AddBill_Receiv_Date] = @AddBill_Receiv_Date, [dbo].[tbl_case].[Remove_Log] = @Remove_Log " +
                                                "where [dbo].[tbl_case].[Case_Name] = @Case_Id";

                    SqlCommand cmdUpdateCase = new SqlCommand(strSqlUpdateCase, connRN);
                    cmdUpdateCase.CommandType = CommandType.Text;

                    cmdUpdateCase.Parameters.AddWithValue("@ModifiDate", caseDetail.ModificationDate);
                    cmdUpdateCase.Parameters.AddWithValue("@ModifiStaff", caseDetail.ModifyingStaff);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form", caseDetail.NPF_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_File_Name", caseDetail.NPF_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@NPF_Form_Destination_File_Name", caseDetail.NPF_Form_Destination_File_Name);
                    if (caseDetail.NPF_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", caseDetail.NPF_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@NPF_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form", caseDetail.IB_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_File_Name", caseDetail.IB_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@IB_Form_Destination_File_Name", caseDetail.IB_Form_Destination_File_Name);
                    if (caseDetail.IB_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", caseDetail.IB_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@IB_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form", caseDetail.POP_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_File_Name", caseDetail.POP_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@POP_Form_Destination_File_Name", caseDetail.POP_Form_Destionation_File_Name);
                    if (caseDetail.POP_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@POP_Receiv_Date", caseDetail.POP_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@POP_Receive_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form", caseDetail.MedicalRecord_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_File_Name", caseDetail.MedRec_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@MedRec_Form_Destination_File_Name", caseDetail.MedRec_Form_Destination_File_Name);
                    if (caseDetail.MedRec_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", caseDetail.MedRec_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@MedRec_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form", caseDetail.Unknown_Form);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_File_Name", caseDetail.Unknown_Form_File_Name);
                    cmdUpdateCase.Parameters.AddWithValue("@Unknown_Form_Destination_File_Name", caseDetail.Unknown_Form_Destination_File_Name);
                    if (caseDetail.Unknown_ReceivedDate != null) cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", caseDetail.Unknown_ReceivedDate);
                    else cmdUpdateCase.Parameters.AddWithValue("@Unknown_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@CaseNote", caseDetail.Note);
                    cmdUpdateCase.Parameters.AddWithValue("@Log_Id", caseDetail.Log_Id);
                    cmdUpdateCase.Parameters.AddWithValue("@AddBill_Form", caseDetail.AddBill_Form);
                    if (caseDetail.AddBill_Received_Date != null) cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", caseDetail.AddBill_Received_Date);
                    else cmdUpdateCase.Parameters.AddWithValue("@AddBill_Receiv_Date", DBNull.Value);

                    cmdUpdateCase.Parameters.AddWithValue("@Remove_Log", caseDetail.Remove_Log);

                    cmdUpdateCase.Parameters.AddWithValue("@Case_Id", caseDetail.CaseId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowAffected = cmdUpdateCase.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowAffected == 1)
                    {
                        MessageBox.Show("The change has been saved.", "Information");

                        btnNewMedBill_Case.Enabled = true;
                        btnEditMedBill.Enabled = true;
                        btnDeleteMedBill.Enabled = true;
                    }
                    else if (nRowAffected == 0) MessageBox.Show("The change has not been saved.", "Error");

                }

                tbCMMManager.TabPages.Remove(tbpgCreateCase);
                tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCaseView"];
                return;
            }
            else if (dlgResult == DialogResult.No)
            {
                tbCMMManager.TabPages.Remove(tbpgCreateCase);
                tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCaseView"];
                return;
            }
            else if (dlgResult == DialogResult.Cancel)
            {
                return;
            }
        }

        private void btnMedBillCreationPgLowerSave_Click(object sender, EventArgs e)
        {
            frmSaveNewMedBill frmSaveMedBill = new frmSaveNewMedBill();

            frmSaveMedBill.StartPosition = FormStartPosition.CenterParent;
            DialogResult dlgResult = frmSaveMedBill.ShowDialog();

            if (dlgResult == DialogResult.Yes)
            {
                String strMedBillNo = txtMedBillNo.Text.Trim();

                String strSqlQueryForMedBill = "select [dbo].[tbl_medbill].[BillNo] from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";

                SqlCommand cmdQueryForMedBill = new SqlCommand(strSqlQueryForMedBill, connRN);
                cmdQueryForMedBill.Parameters.AddWithValue("@MedBillNo", strMedBillNo);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                Object ResultMedBillNo = cmdQueryForMedBill.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                if (ResultMedBillNo == null)
                {
                    String strIndividualId = String.Empty;
                    String strCaseId = String.Empty;
                    String strBillStatus = String.Empty;
                    String strIllnessId = String.Empty;
                    String strIncidentId = String.Empty;

                    String strNewMedBillNo = String.Empty;
                    String strMedProvider = String.Empty;
                    String strPrescriptionName = String.Empty;
                    String strPrescriptionNo = String.Empty;
                    String strPrescriptionDescription = String.Empty;

                    if (txtIndividualIDMedBill.Text.Trim() != String.Empty) strIndividualId = txtIndividualIDMedBill.Text.Trim();
                    if (txtMedBill_CaseNo.Text.Trim() != String.Empty) strCaseId = txtMedBill_CaseNo.Text.Trim();
                    //if (txtMedicalBillStatus.Text.Trim() != String.Empty) strBillStatus = txtMedicalBillStatus.Text.Trim();

                    if (txtMedBill_Illness.Text.Trim() != String.Empty) strIllnessId = Illness.IllnessId;
                    if (txtMedBill_Incident.Text.Trim() != String.Empty) strIncidentId = txtMedBill_Incident.Text.Trim();

                    if (txtMedBillNo.Text.Trim() != String.Empty) strNewMedBillNo = txtMedBillNo.Text.Trim();

                    String MedicalProvider = String.Empty;

                    if (txtMedicalProvider.Text.Trim() != String.Empty)
                    {
                        MedicalProvider = txtMedicalProvider.Text.Trim();
                    }
                    else
                    {
                        MessageBox.Show("Please enter the name of medical provider.", "Error");
                        return;
                    }

                    String PrescriptionName = String.Empty;

                    if (txtPrescriptionName.Text.Trim() != String.Empty)
                    {
                        PrescriptionName = txtPrescriptionName.Text.Trim();
                    }

                    String PrescriptionNo = String.Empty;

                    if (txtNumberOfMedication.Text.Trim() != String.Empty)
                    {
                        PrescriptionNo = txtNumberOfMedication.Text.Trim();
                    }

                    String PrescriptionDescription = String.Empty;

                    if (txtPrescriptionDescription.Text.Trim() != String.Empty)
                    {
                        PrescriptionDescription = txtPrescriptionDescription.Text.Trim();
                    }


                    int nPatientType = 0;   // default outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    //int nSelectedMedNote = cbMedicalBillNote1.SelectedIndex;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }



                    String strSqlInsertNewMedBill = "insert into dbo.tbl_medbill (IsDeleted, BillNo, MedBillType_Id, BillStatus, CreatedDate, CreatedById, LastModifiedDate, LastModifiedById, " +
                                                    "LastActivityDate, LastViewedDate, LastReferencedDate, Case_Id, Incident_Id, Illness_Id, BillAmount, SettlementTotal, " +
                                                    "Balance, BillDate, TotalSharedAmount, Individual_Id, Contact_Id, MedicalProvider_Id, PendingReason, " +
                                                    "Account_At_Provider, ProviderPhoneNumber, ProviderContactPerson, " +
                                                    "ProposalLetterSentDate, HIPPASentDate, MedicalRecordDate, " +
                                                    "BillStatus, ProofOfPaymentReceivedDate, IneligibleReason, OriginalPrescription, PersonalResponsibilityCredit, " +
                                                    "WellBeingCareTotal, WellBeingCare, Memo, DueDate, TotalNumberOfPhysicalTherapy, " +
                                                    "PrescriptionDrugName, PrescriptionNo, PrescriptionDescription, " +
                                                    "PatientTypeId, Note) " +
                                                    "values (@IsDeleted, @BillNo, @MedBillType_Id, @MedBillStatus, @CreatedDate, @CreateById, @LastModifiedDate, @LastModifiedById, " +
                                                    "@LastActivityDate, @LastViewedDate, @LastReferencedDate, @Case_Id, @Incident_Id, @Illness_Id, @BillAmount, @SettlementTotal, " +
                                                    "@Balance, @BillDate, @TotalSharedAmount, @Individual_Id, @Contact_Id, @MedicalProvider_Id, @PendingReason, " +
                                                    "@Account_At_Provider, @ProviderPhoneNo, @ProviderContactPerson, " +
                                                    "@ProposalLetterSentDate, @HIPPASentDate, @MedicalRecordDate, " +
                                                    "@BillStatus, @ProofOfPaymentReceivedDate, @IneligibleReason, @OriginalPrescription, @PersonalResponsibilityCredit, " +
                                                    "@WellBeingCareTotal, @WellBeingCare, @Memo, @DueDate, @TotalNumberOfPhysicalTherapy, " +
                                                    "@PrescriptionDrugName, @PrescriptionNo, @PrescriptionDescription, " +
                                                    "@PatientTypeId, @Note)";

                    SqlCommand cmdInsertNewMedBill = new SqlCommand(strSqlInsertNewMedBill, connRN);
                    cmdInsertNewMedBill.CommandType = CommandType.Text;

                    cmdInsertNewMedBill.Parameters.AddWithValue("@IsDeleted", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillNo", strNewMedBillNo);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillStatus", comboMedBillStatus.SelectedIndex);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreatedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreateById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastViewedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastReferencedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Case_Id", strCaseId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Incident_Id", strIncidentId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Illness_Id", strIllnessId);


                    Decimal BillAmountResult = 0;
                    Decimal BillAmount = 0;

                    if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BillAmountResult))
                    {
                        BillAmount = BillAmountResult;
                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillAmount", BillAmount);
                    }
                    else
                    {
                        MessageBox.Show("Bill Amount is invalid.", "Error");
                        return;
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@SettlementTotal", 0);

                    Decimal BalanceResult = 0;
                    Decimal Balance = 0;

                    if (Decimal.TryParse(txtBalance.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BalanceResult))
                    {
                        Balance = BalanceResult;
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Balance", Balance);
                    }
                    else
                    {
                        MessageBox.Show("Balance is invalid.", "Error");
                        return;
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillDate", dtpBillDate.Value);

                    Decimal TotalSharedAmount = 0;
                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if ((gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "CMM Provider Payment") ||
                            (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Member Reimbursement"))
                            TotalSharedAmount += Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                        if (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Medical Provider Refund")
                            TotalSharedAmount -= Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@TotalSharedAmount", TotalSharedAmount);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Individual_Id", strIndividualId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Contact_Id", strIndividualId);
                    foreach (MedicalProviderInfo info in lstMedicalProvider)
                    {
                        if (info.Name == txtMedicalProvider.Text.Trim())
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalProvider_Id", info.ID);
                            break;
                        }
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@Account_At_Provider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@HIPPASentDate", dtpHippaSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalRecordDate", dtpMedicalRecordDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillStatus", comboMedBillStatus.SelectedIndex);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProofOfPaymentReceivedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@OriginalPrescription", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@PersonalResponsibilityCredit", 500);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCareTotal", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCare", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Memo", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@DueDate", DateTime.Today);

                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type : Medical Bill
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", nPatientType);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", comboPendingReason.SelectedIndex);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 1)   // Medical Bill Type : Prescription
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", txtPrescriptionDescription.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 2)   // Medical Bill Type : Physical Therapy
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short result = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                        {
                            nNumberOfPhysicalTherapy = result;
                            cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else
                        {
                            MessageBox.Show("Please enter a positive integer in the Number of Physical Therapy Text Box.", "Alert");
                            return;
                        }

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowInserted = cmdInsertNewMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowInserted == 1)
                    {
                        MessageBox.Show("The Medical Bill has been saved.", "Information");
                        btnAddNewSettlement.Enabled = true;
                        return;

                    }
                    else if (nRowInserted == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been saved.", "Error");
                        return;
                    }

                    bIsModified = false;

                }
                else if (ResultMedBillNo.ToString() == strMedBillNo)
                {
                    // update the med bill

                    if (txtIndividualIDMedBill.Text.Trim() == String.Empty)
                    {
                        MessageBox.Show("There is no illness code.", "Alert");
                        return;
                    }

                    if (txtMedBill_Incident.Text.Trim() == String.Empty)
                    {
                        MessageBox.Show("There is no incident id.", "Alert");
                        return;
                    }

                    String MedBillNo = txtMedBillNo.Text.Trim();
                    String IndividualId = txtIndividualIDMedBill.Text.Trim();

                    // Get illness id for ICD 10 Code
                    String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] " +
                                                        "where [dbo].[tbl_illness].[Individual_Id] = @IndividualId and [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                    SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                    cmdQueryForIllnessId.CommandType = CommandType.Text;

                    cmdQueryForIllnessId.Parameters.AddWithValue("@IndividualId", IndividualId);
                    cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", txtMedBill_Illness.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //int nIllnessId = Int32.Parse(cmdQueryForIllnessId.ExecuteScalar().ToString());
                    Object objIllnessId = cmdQueryForIllnessId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    int nResult;
                    int? nIllnessId = null;
                    if (objIllnessId != null)
                    {
                        if (Int32.TryParse(objIllnessId.ToString(), NumberStyles.Integer, new CultureInfo("en-US"), out nResult)) nIllnessId = nResult;
                    }
                    //else
                    //{
                    //    MessageBox.Show("Illness Id is empty.", "Alert");
                    //    return;
                    //}

                    // Get medical provider id
                    String strSqlQueryForMedicalProviderId = "select [dbo].[tbl_MedicalProvider].[ID] from [dbo].[tbl_MedicalProvider] where [dbo].[tbl_MedicalProvider].[Name] = @MedicalProviderName";

                    SqlCommand cmdQueryForMedicalProviderId = new SqlCommand(strSqlQueryForMedicalProviderId, connRN);
                    cmdQueryForMedicalProviderId.CommandType = CommandType.Text;

                    cmdQueryForMedicalProviderId.Parameters.AddWithValue("@MedicalProviderName", txtMedicalProvider.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String MedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar().ToString();
                    Object objMedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String MedicalProviderId = String.Empty;

                    if (objMedicalProviderId != null) MedicalProviderId = objMedicalProviderId.ToString();

                    int nPatientType = 0;   // default outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }

                    // Update the Medical Bill
                    String strSqlUpdateMedBill = "update [dbo].[tbl_medbill] set [dbo].[tbl_medbill].[LastModifiedDate] = @NewLastModifiedDate, " +
                                                 "[dbo].[tbl_medbill].[LastModifiedById] = @NewLastModifiedById, " +
                                                     "[dbo].[tbl_medbill].[Case_Id] = @NewCaseId, [dbo].[tbl_medbill].[Incident_Id] = @NewIncidentId, " +
                                                     "[dbo].[tbl_medbill].[Illness_Id] = @NewIllnessId, " +
                                                     "[dbo].[tbl_medbill].[BillAmount] = @NewBillAmount, [dbo].[tbl_medbill].[MedBillType_Id] = @NewMedBillType_Id, " +
                                                     "[dbo].[tbl_medbill].[BillStatus] = @NewMedBillStatus, " +
                                                     "[dbo].[tbl_medbill].[SettlementTotal] = @NewSettlementTotal, [dbo].[tbl_medbill].[Balance] = @NewBalance, " +
                                                     "[dbo].[tbl_medbill].[BillDate] = @NewBillDate, [dbo].[tbl_medbill].[DueDate] = @NewDueDate, [dbo].[tbl_medbill].[TotalSharedAmount] = @NewTotalSharedAmount, " +
                                                     "[dbo].[tbl_medbill].[Guarantor] = @NewGuarantor, " +
                                                     "[dbo].[tbl_medbill].[MedicalProvider_Id] = @NewMedicalProviderId, " +
                                                     "[dbo].[tbl_medbill].[Account_At_Provider] = @NewAccountAtProvider, " +
                                                     "[dbo].[tbl_medbill].[ProviderPhoneNumber] = @NewProviderPhoneNo, " +
                                                     "[dbo].[tbl_medbill].[ProviderContactPerson] = @NewProviderContactPerson, " +
                                                     "[dbo].[tbl_medbill].[ProposalLetterSentDate] = @NewProposalLetterSentDate, " +
                                                     "[dbo].[tbl_medbill].[HIPPASentDate] = @NewHIPPASentDate, " +
                                                     "[dbo].[tbl_medbill].[MedicalRecordDate] = @NewMedicalRecordDate, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDrugName] = @NewPrescriptionDrugName, [dbo].[tbl_medbill].[PrescriptionNo] = @NewPrescriptionNo, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDescription] = @NewPrescriptionDescription, " +
                                                     "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy] = @NewTotalNumberOfPhysicalTherapy, " +
                                                     "[dbo].[tbl_medbill].[PatientTypeId] = @NewPatientTypeId, " +
                                                     "[dbo].[tbl_medbill].[Note] = @Note, " +
                                                     "[dbo].[tbl_medbill].[WellBeingCareTotal] = @NewWellBeingCareTotal, [dbo].[tbl_medbill].[WellBeingCare] = @NewWellBeingCare, " +
                                                     "[dbo].[tbl_medbill].[IneligibleReason] = @NewIneligibleReason, [dbo].[tbl_medbill].[PendingReason] = @NewPendingReason, " +
                                                     "[dbo].[tbl_medbill].[OriginalPrescription] = @NewOriginalPrescription " +
                                                     "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

                    SqlCommand cmdUpdateMedBill = new SqlCommand(strSqlUpdateMedBill, connRN);
                    cmdUpdateMedBill.CommandType = CommandType.Text;

                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedDate", DateTime.Today.ToString("MM/dd/yyyy"));
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedById", nLoggedUserId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewCaseId", txtMedBill_CaseNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIncidentId", txtMedBill_Incident.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIllnessId", nIllnessId.Value);
                    Decimal BillAmount = 0;
                    Decimal BillAmountResult = 0;

                    if (Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out BillAmountResult))
                    {
                        BillAmount = BillAmountResult;
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewBillAmount", BillAmount);
                    }
                    else
                    {
                        MessageBox.Show("Bill Amount is invalid.", "Error");
                        return;
                    }
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillStatus", comboMedBillStatus.SelectedIndex);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewSettlementTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBalance", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBillDate", dtpBillDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewDueDate", dtpDueDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalSharedAmount", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewGuarantor", txtMedBillGuarantor.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalProviderId", MedicalProviderId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewAccountAtProvider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewHIPPASentDate", dtpHippaSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalRecordDate", dtpMedicalRecordDate.Value);

                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type - Medical Bill
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", nPatientType);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", comboPendingReason.SelectedIndex);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);

                    }
                    if (comboMedBillType.SelectedIndex == 1)        // Medical Bill Type - Prescription
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", txtPrescriptionDescription.Text.Trim());

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    if (comboMedBillType.SelectedIndex == 2)        // Medical Bill Type - Physical Therapy
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short NumPhysicalTherapyResult = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out NumPhysicalTherapyResult))
                        {
                            nNumberOfPhysicalTherapy = NumPhysicalTherapyResult;
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else
                        {
                            MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Error");
                            return;
                        }

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }


                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCareTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCare", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewOriginalPrescription", String.Empty);
                    cmdUpdateMedBill.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                    cmdUpdateMedBill.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nAffectedRow = cmdUpdateMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nAffectedRow == 1)
                    {
                        MessageBox.Show("The Medical Bill has been updated.", "Information");
                        return;
                    }
                    else if (nAffectedRow == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been updated.", "Error");
                        return;
                    }

                    bIsModified = false;
                }
            }
            else if (dlgResult == DialogResult.No)
            {
                //tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                //tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];
                return;
            }
        }

        private void btnMedBillCreationPgLowerCancel_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Do you want save the change?", "Alert", MessageBoxButtons.YesNoCancel);

            if (dlgResult == DialogResult.Yes)
            {
                String strMedBillNo = txtMedBillNo.Text.Trim();

                String strSqlQueryForMedBill = "select [dbo].[tbl_medbill].[BillNo] from [dbo].[tbl_medbill] where [dbo].[tbl_medbill].[BillNo] = @MedBillNo";

                SqlCommand cmdQueryForMedBill = new SqlCommand(strSqlQueryForMedBill, connRN);
                cmdQueryForMedBill.Parameters.AddWithValue("@MedBillNo", strMedBillNo);

                //if (connRN.State == ConnectionState.Closed) connRN.Open();
                if (connRN.State == ConnectionState.Open)
                {
                    connRN.Close();
                    connRN.Open();
                }
                else if (connRN.State == ConnectionState.Closed) connRN.Open();
                Object ResultMedBillNo = cmdQueryForMedBill.ExecuteScalar();
                if (connRN.State == ConnectionState.Open) connRN.Close();

                if (ResultMedBillNo == null)
                {
                    String strIndividualId = String.Empty;
                    String strCaseId = String.Empty;
                    String strBillStatus = String.Empty;
                    String strIllnessId = String.Empty;
                    String strIncidentId = String.Empty;

                    String strNewMedBillNo = String.Empty;
                    String strMedProvider = String.Empty;
                    String strPrescriptionName = String.Empty;
                    String strPrescriptionNo = String.Empty;
                    String strPrescriptionDescription = String.Empty;

                    if (txtIndividualIDMedBill.Text.Trim() != String.Empty) strIndividualId = txtIndividualIDMedBill.Text.Trim();
                    if (txtMedBill_CaseNo.Text.Trim() != String.Empty) strCaseId = txtMedBill_CaseNo.Text.Trim();
                    //if (txtMedicalBillStatus.Text.Trim() != String.Empty) strBillStatus = txtMedicalBillStatus.Text.Trim();

                    if (txtMedBill_Illness.Text.Trim() != String.Empty) strIllnessId = Illness.IllnessId;
                    if (txtMedBill_Incident.Text.Trim() != String.Empty) strIncidentId = txtMedBill_Incident.Text.Trim();

                    if (txtMedBillNo.Text.Trim() != String.Empty) strNewMedBillNo = txtMedBillNo.Text.Trim();

                    String MedicalProvider = String.Empty;

                    if (txtMedicalProvider.Text.Trim() != String.Empty)
                    {
                        MedicalProvider = txtMedicalProvider.Text.Trim();
                    }

                    String PrescriptionName = String.Empty;

                    if (txtPrescriptionName.Text.Trim() != String.Empty)
                    {
                        PrescriptionName = txtPrescriptionName.Text.Trim();
                    }

                    String PrescriptionNo = String.Empty;

                    if (txtNumberOfMedication.Text.Trim() != String.Empty)
                    {
                        PrescriptionNo = txtNumberOfMedication.Text.Trim();
                    }

                    String PrescriptionDescription = String.Empty;

                    if (txtPrescriptionDescription.Text.Trim() != String.Empty)
                    {
                        PrescriptionDescription = txtPrescriptionDescription.Text.Trim();
                    }


                    int nPatientType = 0;   // default outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    //int nSelectedMedNote = cbMedicalBillNote1.SelectedIndex;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }



                    String strSqlInsertNewMedBill = "insert into dbo.tbl_medbill (BillNo, MedBillType_Id, CreatedDate, CreatedById, LastModifiedDate, LastModifiedById, " +
                                                    "LastActivityDate, LastViewedDate, LastReferencedDate, Case_Id, Incident_Id, Illness_Id, BillAmount, SettlementTotal, " +
                                                    "Balance, BillDate, TotalSharedAmount, Individual_Id, Contact_Id, MedicalProvider_Id, PendingReason, " +
                                                    "Account_At_Provider, ProviderPhoneNumber, ProviderContactPerson, " +
                                                    "ProposalLetterSentDate, HIPPASentDate, MedicalRecordDate, " +
                                                    "BillStatus, ProofOfPaymentReceivedDate, IneligibleReason, OriginalPrescription, PersonalResponsibilityCredit, " +
                                                    "WellBeingCareTotal, WellBeingCare, Memo, DueDate, TotalNumberOfPhysicalTherapy, " +
                                                    "PrescriptionDrugName, PrescriptionNo, PrescriptionDescription, " +
                                                    "PatientTypeId, Note) " +
                                                    "values (@BillNo, @MedBillType_Id, @CreatedDate, @CreateById, @LastModifiedDate, @LastModifiedById, " +
                                                    "@LastActivityDate, @LastViewedDate, @LastReferencedDate, @Case_Id, @Incident_Id, @Illness_Id, @BillAmount, @SettlementTotal, " +
                                                    "@Balance, @BillDate, @TotalSharedAmount, @Individual_Id, @Contact_Id, @MedicalProvider_Id, @PendingReason, " +
                                                    "@Account_At_Provider, @ProviderPhoneNo, @ProviderContactPerson, " +
                                                    "@ProposalLetterSentDate, @HIPPASentDate, @MedicalRecordDate, " +
                                                    "@BillStatus, @ProofOfPaymentReceivedDate, @IneligibleReason, @OriginalPrescription, @PersonalResponsibilityCredit, " +
                                                    "@WellBeingCareTotal, @WellBeingCare, @Memo, @DueDate, @TotalNumberOfPhysicalTherapy, " +
                                                    "@PrescriptionDrugName, @PrescriptionNo, @PrescriptionDescription, " +
                                                    "@PatientTypeId, @Note)";

                    SqlCommand cmdInsertNewMedBill = new SqlCommand(strSqlInsertNewMedBill, connRN);
                    cmdInsertNewMedBill.CommandType = CommandType.Text;

                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillNo", strNewMedBillNo);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreatedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@CreateById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastModifiedById", nLoggedUserId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastActivityDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastViewedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@LastReferencedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Case_Id", strCaseId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Incident_Id", strIncidentId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Illness_Id", strIllnessId);
                    Decimal dBillAmount = 0;

                    if (!Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out dBillAmount))
                    {
                        MessageBox.Show("Bill Amount should be currency value.", "Error");
                        return;
                    }
                    else
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@BillAmount", Decimal.Parse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US")));
                    }
                    cmdInsertNewMedBill.Parameters.AddWithValue("@SettlementTotal", 0);

                    Decimal dBalance = 0;
                    if (!Decimal.TryParse(txtMedBillAmount.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US"), out dBalance))
                    {
                        MessageBox.Show("Balance should be currency value.", "Error");
                    }
                    else
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Balance", Decimal.Parse(txtBalance.Text.Trim(), NumberStyles.Currency, new CultureInfo("en-US")));
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillDate", dtpBillDate.Value);

                    Decimal TotalSharedAmount = 0;
                    for (int i = 0; i < gvSettlementsInMedBill.Rows.Count; i++)
                    {
                        if ((gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "CMM Provider Payment") ||
                            (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Member Reimbursement"))
                            TotalSharedAmount += Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                        if (gvSettlementsInMedBill["SettlementType", i].Value.ToString() == "Medical Provider Refund")
                            TotalSharedAmount -= Decimal.Parse(gvSettlementsInMedBill["SettlementType", i].Value.ToString(), NumberStyles.Currency, new CultureInfo("en-US"));
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@TotalSharedAmount", TotalSharedAmount);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Individual_Id", strIndividualId);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Contact_Id", strIndividualId);
                    foreach (MedicalProviderInfo info in lstMedicalProvider)
                    {
                        if (info.Name == txtMedicalProvider.Text.Trim())
                        {
                            cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalProvider_Id", info.ID);
                            break;
                        }
                    }

                    cmdInsertNewMedBill.Parameters.AddWithValue("@Account_At_Provider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@HIPPASentDate", dtpHippaSentDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@MedicalRecordDate", dtpMedicalRecordDate.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@BillStatus", comboMedBillStatus.SelectedIndex);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@ProofOfPaymentReceivedDate", DateTime.Today);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@OriginalPrescription", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@PersonalResponsibilityCredit", 500);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCareTotal", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@WellBeingCare", 0);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@Memo", DBNull.Value);
                    cmdInsertNewMedBill.Parameters.AddWithValue("@DueDate", DateTime.Today);

                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type : Medical Bill
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", nPatientType);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", comboPendingReason.SelectedIndex);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 1)   // Medical Bill Type : Prescription
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", txtPrescriptionDescription.Text.Trim());
                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    else if (comboMedBillType.SelectedIndex == 2)   // Medical Bill Type : Physical Therapy
                    {
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDrugName", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionNo", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PrescriptionDescription", DBNull.Value);

                        cmdInsertNewMedBill.Parameters.AddWithValue("@PatientTypeId", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@PendingReason", DBNull.Value);
                        cmdInsertNewMedBill.Parameters.AddWithValue("@IneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short result = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                        {
                            nNumberOfPhysicalTherapy = result;
                            cmdInsertNewMedBill.Parameters.AddWithValue("@TotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Alert");

                        cmdInsertNewMedBill.Parameters.AddWithValue("@Note", strNote);
                    }

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nRowInserted = cmdInsertNewMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nRowInserted == 1)
                    {
                        MessageBox.Show("The Medical Bill has been saved.", "Information");
                    }
                    else if (nRowInserted == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been saved.", "Error");
                    }

                    bIsModified = false;

                    tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                    tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];

                }
                else if (ResultMedBillNo.ToString() == strMedBillNo)
                {
                    // update the med bill

                    String MedBillNo = txtMedBillNo.Text.Trim();
                    String IndividualId = txtIndividualIDMedBill.Text.Trim();

                    // Get illness id for ICD 10 Code
                    String strSqlQueryForIllnessId = "select [dbo].[tbl_illness].[Illness_Id] from [dbo].[tbl_illness] " +
                                                        "where [dbo].[tbl_illness].[Individual_Id] = @IndividualId and [dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";

                    SqlCommand cmdQueryForIllnessId = new SqlCommand(strSqlQueryForIllnessId, connRN);
                    cmdQueryForIllnessId.CommandType = CommandType.Text;

                    cmdQueryForIllnessId.Parameters.AddWithValue("@IndividualId", IndividualId);
                    cmdQueryForIllnessId.Parameters.AddWithValue("@ICD10Code", txtMedBill_Illness.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //int nIllnessId = Int32.Parse(cmdQueryForIllnessId.ExecuteScalar().ToString());
                    Object objIllnessId = cmdQueryForIllnessId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    int? nIllnessId = null;

                    if (objIllnessId != null)
                    {
                        int IllnessIdResult = 0;
                        if (Int32.TryParse(objIllnessId.ToString(), NumberStyles.Integer, new CultureInfo("en-US"), out IllnessIdResult)) nIllnessId = IllnessIdResult;
                    }

                    // Get medical provider id
                    String strSqlQueryForMedicalProviderId = "select [dbo].[tbl_MedicalProvider].[ID] from [dbo].[tbl_MedicalProvider] where [dbo].[tbl_MedicalProvider].[Name] = @MedicalProviderName";

                    SqlCommand cmdQueryForMedicalProviderId = new SqlCommand(strSqlQueryForMedicalProviderId, connRN);
                    cmdQueryForMedicalProviderId.CommandType = CommandType.Text;

                    cmdQueryForMedicalProviderId.Parameters.AddWithValue("@MedicalProviderName", txtMedicalProvider.Text.Trim());

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    //String MedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar().ToString();
                    Object objMedicalProviderId = cmdQueryForMedicalProviderId.ExecuteScalar();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    String MedicalProviderId = String.Empty;

                    if (objMedicalProviderId != null) MedicalProviderId = objMedicalProviderId.ToString();

                    int nPatientType = 0;   // default is outpatient

                    if (rbOutpatient.Checked) nPatientType = 0;
                    else if (rbInpatient.Checked) nPatientType = 1;

                    String strNote = String.Empty;

                    if (comboMedBillType.SelectedItem.ToString() == "Medical Bill")
                    {
                        strNote = txtMedBillNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Prescription")
                    {
                        strNote = txtPrescriptionNote.Text.Trim();
                    }
                    else if (comboMedBillType.SelectedItem.ToString() == "Physical Therapy")
                    {
                        strNote = txtPhysicalTherapyRxNote.Text.Trim();
                    }

                    // Update the Medical Bill
                    String strSqlUpdateMedBill = "update [dbo].[tbl_medbill] set [dbo].[tbl_medbill].[LastModifiedDate] = @NewLastModifiedDate, [dbo].[tbl_medbill].[LastModifiedById] = @NewLastModifiedById, " +
                                                     "[dbo].[tbl_medbill].[Case_Id] = @NewCaseId, [dbo].[tbl_medbill].[Incident_Id] = @NewIncidentId, [dbo].[tbl_medbill].[Illness_Id] = @NewIllnessId, " +
                                                     "[dbo].[tbl_medbill].[BillAmount] = @NewBillAmount, [dbo].[tbl_medbill].[MedBillType_Id] = @NewMedBillType_Id, " +
                                                     "[dbo].[tbl_medbill].[SettlementTotal] = @NewSettlementTotal, [dbo].[tbl_medbill].[Balance] = @NewBalance, " +
                                                     "[dbo].[tbl_medbill].[BillDate] = @NewBillDate, [dbo].[tbl_medbill].[DueDate] = @NewDueDate, [dbo].[tbl_medbill].[TotalSharedAmount] = @NewTotalSharedAmount, " +
                                                     "[dbo].[tbl_medbill].[Guarantor] = @NewGuarantor, " +
                                                     "[dbo].[tbl_medbill].[MedicalProvider_Id] = @NewMedicalProviderId, " +
                                                     "[dbo].[tbl_medbill].[Account_At_Provider] = @NewAccountAtProvider, " +
                                                     "[dbo].[tbl_medbill].[ProviderPhoneNumber] = @NewProviderPhoneNo, " +
                                                     "[dbo].[tbl_medbill].[ProviderContactPerson] = @NewProviderContactPerson, " +
                                                     "[dbo].[tbl_medbill].[ProposalLetterSentDate] = @NewProposalLetterSentDate, " +
                                                     "[dbo].[tbl_medbill].[HIPPASentDate] = @NewHIPPASentDate, " +
                                                     "[dbo].[tbl_medbill].[MedicalRecordDate] = @NewMedicalRecordDate, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDrugName] = @NewPrescriptionDrugName, [dbo].[tbl_medbill].[PrescriptionNo] = @NewPrescriptionNo, " +
                                                     "[dbo].[tbl_medbill].[PrescriptionDescription] = @NewPrescriptionDescription, " +
                                                     "[dbo].[tbl_medbill].[TotalNumberOfPhysicalTherapy] = @NewTotalNumberOfPhysicalTherapy, " +
                                                     "[dbo].[tbl_medbill].[PatientTypeId] = @NewPatientTypeId, " +
                                                     "[dbo].[tbl_medbill].[Note] = @Note, " +
                                                     "[dbo].[tbl_medbill].[WellBeingCareTotal] = @NewWellBeingCareTotal, [dbo].[tbl_medbill].[WellBeingCare] = @NewWellBeingCare, " +
                                                     "[dbo].[tbl_medbill].[IneligibleReason] = @NewIneligibleReason, [dbo].[tbl_medbill].[PendingReason] = @NewPendingReason, " +
                                                     "[dbo].[tbl_medbill].[OriginalPrescription] = @NewOriginalPrescription " +
                                                     "where [dbo].[tbl_medbill].[BillNo] = @MedBillNo and [dbo].[tbl_medbill].[Contact_Id] = @IndividualId";

                    SqlCommand cmdUpdateMedBill = new SqlCommand(strSqlUpdateMedBill, connRN);
                    cmdUpdateMedBill.CommandType = CommandType.Text;

                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedDate", DateTime.Today.ToString("MM/dd/yyyy"));
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewLastModifiedById", nLoggedUserId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewCaseId", txtMedBill_CaseNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIncidentId", txtMedBill_Incident.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewIllnessId", nIllnessId.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBillAmount", Decimal.Parse(txtMedBillAmount.Text.Substring(1).Trim()));
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedBillType_Id", comboMedBillType.SelectedIndex + 1);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewSettlementTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBalance", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewBillDate", dtpBillDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewDueDate", dtpDueDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalSharedAmount", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewGuarantor", txtMedBillGuarantor.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalProviderId", MedicalProviderId);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewAccountAtProvider", txtMedBillAccountNoAtProvider.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderPhoneNo", txtMedProviderPhoneNo.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProviderContactPerson", txtProviderContactPerson.Text.Trim());
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewProposalLetterSentDate", dtpProposalLetterSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewHIPPASentDate", dtpHippaSentDate.Value);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewMedicalRecordDate", dtpMedicalRecordDate.Value);

                    if (comboMedBillType.SelectedIndex == 0)        // Medical Bill Type - Medical Bill
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", nPatientType);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", comboPendingReason.SelectedIndex);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", comboIneligibleReason.SelectedIndex);

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);

                    }
                    if (comboMedBillType.SelectedIndex == 1)        // Medical Bill Type - Prescription
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", txtPrescriptionName.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", txtNumberOfMedication.Text.Trim());
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", txtPrescriptionDescription.Text.Trim());

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }
                    if (comboMedBillType.SelectedIndex == 2)        // Medical Bill Type - Physical Therapy
                    {
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDrugName", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionNo", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPrescriptionDescription", DBNull.Value);

                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPatientTypeId", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewPendingReason", DBNull.Value);
                        cmdUpdateMedBill.Parameters.AddWithValue("@NewIneligibleReason", DBNull.Value);

                        int nNumberOfPhysicalTherapy = 0;
                        short result = 0;
                        if (Int16.TryParse(txtNumPhysicalTherapy.Text.Trim(), out result))
                        {
                            nNumberOfPhysicalTherapy = result;
                            cmdUpdateMedBill.Parameters.AddWithValue("@NewTotalNumberOfPhysicalTherapy", nNumberOfPhysicalTherapy);
                        }
                        else MessageBox.Show("Please enter a positive integer in Number of Physical Therapy Text Box.", "Alert");

                        cmdUpdateMedBill.Parameters.AddWithValue("@Note", strNote);
                    }


                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCareTotal", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewWellBeingCare", 0);
                    cmdUpdateMedBill.Parameters.AddWithValue("@NewOriginalPrescription", String.Empty);
                    cmdUpdateMedBill.Parameters.AddWithValue("@MedBillNo", MedBillNo);
                    cmdUpdateMedBill.Parameters.AddWithValue("@IndividualId", IndividualId);

                    //if (connRN.State == ConnectionState.Closed) connRN.Open();
                    if (connRN.State == ConnectionState.Open)
                    {
                        connRN.Close();
                        connRN.Open();
                    }
                    else if (connRN.State == ConnectionState.Closed) connRN.Open();
                    int nAffectedRow = cmdUpdateMedBill.ExecuteNonQuery();
                    if (connRN.State == ConnectionState.Open) connRN.Close();

                    if (nAffectedRow == 1)
                    {
                        MessageBox.Show("The Medical Bill has been updated.", "Information");
                    }
                    else if (nAffectedRow == 0)
                    {
                        MessageBox.Show("The Medical Bill has not been updated.", "Error");
                    }

                    bIsModified = false;

                    tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                    tbCMMManager.SelectedIndex = 4;

                }
            }
            else if (dlgResult == DialogResult.No)
            {
                tbCMMManager.TabPages.Remove(tbpgMedicalBill);
                tbCMMManager.SelectedTab = tbCMMManager.TabPages["tbpgCreateCase"];
                return;
            }
            else if (dlgResult == DialogResult.Cancel)
            {
                return;
            }
        }

        private void btnIndViewUpdateUpperRight_Click(object sender, EventArgs e)
        {
            String IndividualIdForUpdate = txtIndividualID.Text.Trim();

            int nPreferredLanguage = 0;

            if (rbKorean.Checked) nPreferredLanguage = 0;
            else if (rbEnglish.Checked) nPreferredLanguage = 1;

            String strSqlUpdateIndividualInfo = "update [dbo].[contact] set [dbo].[contact].[PreferredLanguage] = @PreferredLanguage, " +
                                                "[dbo].[contact].[PreferredCommunicationMethod] = @PreferredCommMethod, " +
                                                "[dbo].[contact].[FirstName] = @FirstName, " +
                                                "[dbo].[contact].[MiddleName] = @MiddleName, " +
                                                "[dbo].[contact].[LastName] = @LastName, " +
                                                "[dbo].[contact].[Birthdate] = @BirthDate, " +
                                                "[dbo].[contact].[CMM_Gender__c] = @Gender, " +
                                                "[dbo].[contact].[Social_Security_Number__c] = @SSN, " +
                                                "[dbo].[contact].[MailingStreet] = @ShippingStreet, " +
                                                "[dbo].[contact].[MailingCity] = @ShippingCity, " +
                                                "[dbo].[contact].[MailingState] = @ShippingState, " +
                                                "[dbo].[contact].[MailingPostalCode] = @ShippingZipCode, " +
                                                "[dbo].[contact].[OtherStreet] = @BillingStreet, " +
                                                "[dbo].[contact].[OtherCity] = @BillingCity, " +
                                                "[dbo].[contact].[OtherState] = @BillingState, " +
                                                "[dbo].[contact].[OtherPostalCode] = @BillingZipCode, " +
                                                "[dbo].[contact].[Email] = @Email, " +
                                                "[dbo].[contact].[Phone] = @Phone, " +
                                                "[dbo].[contact].[HomePhone] = @HomePhone, " +
                                                "[dbo].[contact].[PowerOfAttorney] = @PowerOfAttorney, " +
                                                "[dbo].[contact].[Relationship] = @Relationship, " +
                                                "[dbo].[contact].[ReimbursementMethod] = @ReimbursementMethod, " +
                                                "[dbo].[contact].[c4g_Church__c] = @ChurchId " +
                                                "where [dbo].[contact].[Individual_ID__c] = @IndividualId";

            SqlCommand cmdUpdateIndividualInfo = new SqlCommand(strSqlUpdateIndividualInfo, connSalesforce);
            cmdUpdateIndividualInfo.CommandType = CommandType.Text;

            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredLanguage", nPreferredLanguage);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredCommMethod", cbPreferredCommunication.SelectedIndex);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@MiddleName", txtMiddleName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Gender", cbGender.SelectedIndex);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@SSN", txtIndividualSSN.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingStreet", txtStreetAddress1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingCity", txtCity1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingState", txtState1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingZipCode", txtZip1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingStreet", txtStreetAddress2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingCity", txtCity2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingState", txtState2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingZipCode", txtZip2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Phone", txtCellPhone1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@HomePhone", txtBusinessPhone.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PowerOfAttorney", txtPowerOfAttorney.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Relationship", txtRelationship.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ReimbursementMethod", cbPaymentMethod.SelectedIndex);

            if (txtIndChurchName.Text.Trim() != String.Empty)
            {
                foreach (ChurchInfo info in lstChurchInfo)
                {
                    if (info.Name.Trim() == txtIndChurchName.Text.Trim())
                    {
                        cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", info.ID);
                    }
                }
            }
            else cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", DBNull.Value);

            cmdUpdateIndividualInfo.Parameters.AddWithValue("@IndividualId", IndividualIdForUpdate);

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            int nRowAffected = cmdUpdateIndividualInfo.ExecuteNonQuery();
            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            if (nRowAffected == 1)
            {
                MessageBox.Show("The individual information has been updated.", "Information");
                return;
            }
            else if (nRowAffected == 0)
            {
                MessageBox.Show("The individual information has not been updated.", "Error");
                return;
            }
        }

        private void btnIndViewCancelUpperRight_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Do you want to close Individual Page?", "Alert", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                if (tbCMMManager.Contains(tbpgCaseView))
                {
                    MessageBox.Show("Case View page is open. Close Case View page first.", "Alert");
                    return;
                }

                DialogResult dlgSaveResult = MessageBox.Show("Do you want to update the Individual Information?", "Alert", MessageBoxButtons.YesNo);

                if (dlgSaveResult == DialogResult.Yes)
                {
                    // Save individual info
                    String IndividualIdForUpdate = txtIndividualID.Text.Trim();

                    int nPreferredLanguage = 0;

                    if (rbKorean.Checked) nPreferredLanguage = 0;
                    else if (rbEnglish.Checked) nPreferredLanguage = 1;

                    String strSqlUpdateIndividualInfo = "update [dbo].[contact] set [dbo].[contact].[PreferredLanguage] = @PreferredLanguage, " +
                                                        "[dbo].[contact].[PreferredCommunicationMethod] = @PreferredCommMethod, " +
                                                        "[dbo].[contact].[FirstName] = @FirstName, " +
                                                        "[dbo].[contact].[MiddleName] = @MiddleName, " +
                                                        "[dbo].[contact].[LastName] = @LastName, " +
                                                        "[dbo].[contact].[Birthdate] = @BirthDate, " +
                                                        "[dbo].[contact].[CMM_Gender__c] = @Gender, " +
                                                        "[dbo].[contact].[Social_Security_Number__c] = @SSN, " +
                                                        "[dbo].[contact].[MailingStreet] = @ShippingStreet, " +
                                                        "[dbo].[contact].[MailingCity] = @ShippingCity, " +
                                                        "[dbo].[contact].[MailingState] = @ShippingState, " +
                                                        "[dbo].[contact].[MailingPostalCode] = @ShippingZipCode, " +
                                                        "[dbo].[contact].[OtherStreet] = @BillingStreet, " +
                                                        "[dbo].[contact].[OtherCity] = @BillingCity, " +
                                                        "[dbo].[contact].[OtherState] = @BillingState, " +
                                                        "[dbo].[contact].[OtherPostalCode] = @BillingZipCode, " +
                                                        "[dbo].[contact].[Email] = @Email, " +
                                                        "[dbo].[contact].[Phone] = @Phone, " +
                                                        "[dbo].[contact].[HomePhone] = @HomePhone, " +
                                                        "[dbo].[contact].[PowerOfAttorney] = @PowerOfAttorney, " +
                                                        "[dbo].[contact].[Relationship] = @Relationship, " +
                                                        "[dbo].[contact].[ReimbursementMethod] = @ReimbursementMethod, " +
                                                        "[dbo].[contact].[c4g_Church__c] = @ChurchId " +
                                                        "where [dbo].[contact].[Individual_ID__c] = @IndividualId";

                    SqlCommand cmdUpdateIndividualInfo = new SqlCommand(strSqlUpdateIndividualInfo, connSalesforce);
                    cmdUpdateIndividualInfo.CommandType = CommandType.Text;

                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredLanguage", nPreferredLanguage);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredCommMethod", cbPreferredCommunication.SelectedIndex);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@MiddleName", txtMiddleName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Gender", cbGender.SelectedIndex);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@SSN", txtIndividualSSN.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingStreet", txtStreetAddress1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingCity", txtCity1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingState", txtState1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingZipCode", txtZip1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingStreet", txtStreetAddress2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingCity", txtCity2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingState", txtState2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingZipCode", txtZip2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Phone", txtCellPhone1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@HomePhone", txtBusinessPhone.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PowerOfAttorney", txtPowerOfAttorney.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Relationship", txtRelationship.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ReimbursementMethod", cbPaymentMethod.SelectedIndex);

                    if (txtIndChurchName.Text.Trim() != String.Empty)
                    {
                        foreach (ChurchInfo info in lstChurchInfo)
                        {
                            if (info.Name.Trim() == txtIndChurchName.Text.Trim())
                            {
                                cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", info.ID);
                            }
                        }
                    }
                    else cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", DBNull.Value);

                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@IndividualId", IndividualIdForUpdate);

                    if (connSalesforce.State == ConnectionState.Open)
                    {
                        connSalesforce.Close();
                        connSalesforce.Open();
                    }
                    else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

                    int nRowAffected = cmdUpdateIndividualInfo.ExecuteNonQuery();
                    if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                    if (nRowAffected == 1)
                    {
                        MessageBox.Show("The individual information has been updated.", "Information");
                    }
                    else if (nRowAffected == 0)
                    {
                        MessageBox.Show("The individual information has not been updated.", "Error");
                    }
                }
                tbCMMManager.TabPages.Remove(tbpgIndividual);
                tbCMMManager.SelectedIndex = 1;
            }
            else return;

        }

        private void btnIndViewCancelLowerRight_Click(object sender, EventArgs e)
        {
            DialogResult dlgResult = MessageBox.Show("Do you want to close Individual Page?", "Alert", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                if (tbCMMManager.Contains(tbpgCaseView))
                {
                    MessageBox.Show("Case View page is open. Close Case View page first.", "Alert");
                    return;
                }

                DialogResult dlgSaveResult = MessageBox.Show("Do you want to update the Individual Information?", "Alert", MessageBoxButtons.YesNo);

                if (dlgSaveResult == DialogResult.Yes)
                {
                    // Save individual info
                    String IndividualIdForUpdate = txtIndividualID.Text.Trim();

                    int nPreferredLanguage = 0;

                    if (rbKorean.Checked) nPreferredLanguage = 0;
                    else if (rbEnglish.Checked) nPreferredLanguage = 1;

                    String strSqlUpdateIndividualInfo = "update [dbo].[contact] set [dbo].[contact].[PreferredLanguage] = @PreferredLanguage, " +
                                                        "[dbo].[contact].[PreferredCommunicationMethod] = @PreferredCommMethod, " +
                                                        "[dbo].[contact].[FirstName] = @FirstName, " +
                                                        "[dbo].[contact].[MiddleName] = @MiddleName, " +
                                                        "[dbo].[contact].[LastName] = @LastName, " +
                                                        "[dbo].[contact].[Birthdate] = @BirthDate, " +
                                                        "[dbo].[contact].[CMM_Gender__c] = @Gender, " +
                                                        "[dbo].[contact].[Social_Security_Number__c] = @SSN, " +
                                                        "[dbo].[contact].[MailingStreet] = @ShippingStreet, " +
                                                        "[dbo].[contact].[MailingCity] = @ShippingCity, " +
                                                        "[dbo].[contact].[MailingState] = @ShippingState, " +
                                                        "[dbo].[contact].[MailingPostalCode] = @ShippingZipCode, " +
                                                        "[dbo].[contact].[OtherStreet] = @BillingStreet, " +
                                                        "[dbo].[contact].[OtherCity] = @BillingCity, " +
                                                        "[dbo].[contact].[OtherState] = @BillingState, " +
                                                        "[dbo].[contact].[OtherPostalCode] = @BillingZipCode, " +
                                                        "[dbo].[contact].[Email] = @Email, " +
                                                        "[dbo].[contact].[Phone] = @Phone, " +
                                                        "[dbo].[contact].[HomePhone] = @HomePhone, " +
                                                        "[dbo].[contact].[PowerOfAttorney] = @PowerOfAttorney, " +
                                                        "[dbo].[contact].[Relationship] = @Relationship, " +
                                                        "[dbo].[contact].[ReimbursementMethod] = @ReimbursementMethod, " +
                                                        "[dbo].[contact].[c4g_Church__c] = @ChurchId " +
                                                        "where [dbo].[contact].[Individual_ID__c] = @IndividualId";

                    SqlCommand cmdUpdateIndividualInfo = new SqlCommand(strSqlUpdateIndividualInfo, connSalesforce);
                    cmdUpdateIndividualInfo.CommandType = CommandType.Text;

                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredLanguage", nPreferredLanguage);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredCommMethod", cbPreferredCommunication.SelectedIndex);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@MiddleName", txtMiddleName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Gender", cbGender.SelectedIndex);
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@SSN", txtIndividualSSN.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingStreet", txtStreetAddress1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingCity", txtCity1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingState", txtState1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingZipCode", txtZip1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingStreet", txtStreetAddress2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingCity", txtCity2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingState", txtState2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingZipCode", txtZip2.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Phone", txtCellPhone1.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@HomePhone", txtBusinessPhone.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@PowerOfAttorney", txtPowerOfAttorney.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@Relationship", txtRelationship.Text.Trim());
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ReimbursementMethod", cbPaymentMethod.SelectedIndex);
                    foreach (ChurchInfo info in lstChurchInfo)
                    {
                        if (info.Name.Trim() == txtIndChurchName.Text.Trim())
                        {
                            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", info.ID);
                        }
                    }
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@IndividualId", IndividualIdForUpdate);

                    if (connSalesforce.State == ConnectionState.Open)
                    {
                        connSalesforce.Close();
                        connSalesforce.Open();
                    }
                    else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

                    int nRowAffected = cmdUpdateIndividualInfo.ExecuteNonQuery();
                    if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

                    if (nRowAffected == 1)
                    {
                        MessageBox.Show("The individual information has been updated.", "Information");
                    }
                    else if (nRowAffected == 0)
                    {
                        MessageBox.Show("The individual information has not been updated.", "Error");
                    }
                }
                tbCMMManager.TabPages.Remove(tbpgIndividual);
                tbCMMManager.SelectedIndex = 1;
            }
            else return;
        }

        private void btnCloseCaseView_Click(object sender, EventArgs e)
        {
            if (tbCMMManager.Contains(tbpgCreateCase))
            {
                MessageBox.Show("Case Page is open. Close Case Page first.", "Alert");
                return;
            }

            DialogResult dlgResult = MessageBox.Show("Do you want to close Case View?", "Alert", MessageBoxButtons.YesNo);

            if (dlgResult == DialogResult.Yes)
            {
                tbCMMManager.TabPages.Remove(tbpgCaseView);
                tbCMMManager.SelectedIndex = 2;

                return;
            }
            else return;

        }

        private void btnCaseViewIndividual_Click(object sender, EventArgs e)
        {
            if (tbCMMManager.TabPages.Contains(tbpgCaseView))
            {
                MessageBox.Show("Case View Page is already open. Close Case View page first.", "Alert");
                return;
            }
            else
            {
                tbCMMManager.TabPages.Insert(3, tbpgCaseView);
                tbCMMManager.SelectedIndex = 3;
            }
        }

        private void btnIndViewUpdateLowerRight_Click(object sender, EventArgs e)
        {
            String IndividualIdForUpdate = txtIndividualID.Text.Trim();

            int nPreferredLanguage = 0;

            if (rbKorean.Checked) nPreferredLanguage = 0;
            else if (rbEnglish.Checked) nPreferredLanguage = 1;

            String strSqlUpdateIndividualInfo = "update [dbo].[contact] set [dbo].[contact].[PreferredLanguage] = @PreferredLanguage, " +
                                                "[dbo].[contact].[PreferredCommunicationMethod] = @PreferredCommMethod, " +
                                                "[dbo].[contact].[FirstName] = @FirstName, " +
                                                "[dbo].[contact].[MiddleName] = @MiddleName, " +
                                                "[dbo].[contact].[LastName] = @LastName, " +
                                                "[dbo].[contact].[Birthdate] = @BirthDate, " +
                                                "[dbo].[contact].[CMM_Gender__c] = @Gender, " +
                                                "[dbo].[contact].[Social_Security_Number__c] = @SSN, " +
                                                "[dbo].[contact].[MailingStreet] = @ShippingStreet, " +
                                                "[dbo].[contact].[MailingCity] = @ShippingCity, " +
                                                "[dbo].[contact].[MailingState] = @ShippingState, " +
                                                "[dbo].[contact].[MailingPostalCode] = @ShippingZipCode, " +
                                                "[dbo].[contact].[OtherStreet] = @BillingStreet, " +
                                                "[dbo].[contact].[OtherCity] = @BillingCity, " +
                                                "[dbo].[contact].[OtherState] = @BillingState, " +
                                                "[dbo].[contact].[OtherPostalCode] = @BillingZipCode, " +
                                                "[dbo].[contact].[Email] = @Email, " +
                                                "[dbo].[contact].[Phone] = @Phone, " +
                                                "[dbo].[contact].[HomePhone] = @HomePhone, " +
                                                "[dbo].[contact].[PowerOfAttorney] = @PowerOfAttorney, " +
                                                "[dbo].[contact].[Relationship] = @Relationship, " +
                                                "[dbo].[contact].[ReimbursementMethod] = @ReimbursementMethod, " +
                                                "[dbo].[contact].[c4g_Church__c] = @ChurchId " +
                                                "where [dbo].[contact].[Individual_ID__c] = @IndividualId";

            SqlCommand cmdUpdateIndividualInfo = new SqlCommand(strSqlUpdateIndividualInfo, connSalesforce);
            cmdUpdateIndividualInfo.CommandType = CommandType.Text;

            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredLanguage", nPreferredLanguage);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PreferredCommMethod", cbPreferredCommunication.SelectedIndex);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@MiddleName", txtMiddleName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Gender", cbGender.SelectedIndex);
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@SSN", txtIndividualSSN.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingStreet", txtStreetAddress1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingCity", txtCity1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingState", txtState1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ShippingZipCode", txtZip1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingStreet", txtStreetAddress2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingCity", txtCity2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingState", txtState2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@BillingZipCode", txtZip2.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Phone", txtCellPhone1.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@HomePhone", txtBusinessPhone.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@PowerOfAttorney", txtPowerOfAttorney.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@Relationship", txtRelationship.Text.Trim());
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@ReimbursementMethod", cbPaymentMethod.SelectedIndex);
            foreach (ChurchInfo info in lstChurchInfo)
            {
                if (info.Name.Trim() == txtIndChurchName.Text.Trim())
                {
                    cmdUpdateIndividualInfo.Parameters.AddWithValue("@ChurchId", info.ID);
                }
            }
            cmdUpdateIndividualInfo.Parameters.AddWithValue("@IndividualId", IndividualIdForUpdate);

            if (connSalesforce.State == ConnectionState.Open)
            {
                connSalesforce.Close();
                connSalesforce.Open();
            }
            else if (connSalesforce.State == ConnectionState.Closed) connSalesforce.Open();

            int nRowAffected = cmdUpdateIndividualInfo.ExecuteNonQuery();
            if (connSalesforce.State == ConnectionState.Open) connSalesforce.Close();

            if (nRowAffected == 1)
            {
                MessageBox.Show("The individual information has been updated.", "Information");
                return;
            }
            else if (nRowAffected == 0)
            {
                MessageBox.Show("The individual information has not been updated.", "Error");
                return;
            }
        }

        private void chkNPF_CaseCreationPage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk.Checked) btnBrowseNPF.Enabled = true;
            else btnBrowseNPF.Enabled = false;
        }

        private void chkIB_CaseCreationPage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk.Checked) btnBrowseIB.Enabled = true;
            else btnBrowseIB.Enabled = false;
        }

        private void chkPoP_CaseCreationPage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk.Checked) btnBrowsePoP.Enabled = true;
            else btnBrowsePoP.Enabled = false;
        }

        private void chkMedicalRecordCaseCreationPage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk.Checked) btnBrowseMR.Enabled = true;
            else btnBrowseMR.Enabled = false;
        }

        private void chkOtherDocCaseCreationPage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;

            if (chk.Checked) btnBrowseUnknownDoc.Enabled = true;
            else btnBrowseUnknownDoc.Enabled = false;
        }
    }

    public class IncidentProgramInfo
    {

        public Boolean bPersonalResponsibilityProgram;
        public int? IncidentProgramId;
        public String IncidentProgramName;
        public Decimal PersonalResponsibilityAmount;

        public IncidentProgramInfo()
        {
            bPersonalResponsibilityProgram = false;
            IncidentProgramId = null;
            IncidentProgramName = String.Empty;
        }

        public IncidentProgramInfo(int program_id, String program_name)
        {
            IncidentProgramId = program_id;
            IncidentProgramName = program_name;

            switch (program_id)
            {
                case 0:
                    PersonalResponsibilityAmount = 500;
                    break;
                case 1:
                    PersonalResponsibilityAmount = 500;
                    break;
                case 2:
                    PersonalResponsibilityAmount = 1000;
                    break;
                case 3:
                    PersonalResponsibilityAmount = 5000;
                    break;
                case 4:
                    PersonalResponsibilityAmount = 500;
                    break;
                case 5:
                    PersonalResponsibilityAmount = 500;
                    break;
            }
        }
    }

    public class MedBillNoteTypeInfo
    {
        public int? MedBillNoteTypeId;
        public String MedBillNoteTypeValue;

        public MedBillNoteTypeInfo()
        {
            MedBillNoteTypeId = null;
            MedBillNoteTypeValue = String.Empty;
        }

        public MedBillNoteTypeInfo(int id, String value)
        {
            MedBillNoteTypeId = id;
            MedBillNoteTypeValue = value;
        }
    }

    public class MedBillStatusInfo
    {
        public int BillStatusCode;
        public String BillStatusValue;

        public MedBillStatusInfo()
        {
            BillStatusCode = 0;
            BillStatusValue = String.Empty;
        }
    }

    public class MedicalProviderInfo
    {
        public String ID;
        public String Name;
        public String Type;

        public MedicalProviderInfo()
        {
            ID = String.Empty;
            Name = String.Empty;
            Type = String.Empty;
        }

        public MedicalProviderInfo(String id, String name, String type)
        {
            ID = id;
            Name = name;
            Type = type;
        }
    }

    public class ChurchInfo
    {
        public String ID;
        public String Name;

        public ChurchInfo()
        {
            ID = String.Empty;
            Name = String.Empty;
        }

        public ChurchInfo(String id, String name)
        {
            ID = id;
            Name = name;
        }
    }

    public class MedicalBillInfo
    {
        String BillNo;
        DateTime BillDate;
        int BillType;
        String CaseId;
        String Incident;
        String IllnessId;
        Double BillAmount;
        Double SettlementTotal;
        Double TotalSharedAmount;
        int BillStatus;

        public MedicalBillInfo()
        {
            BillNo = String.Empty;
            BillDate = DateTime.Today;
            BillType = -1;
            CaseId = String.Empty;
            Incident = String.Empty;
            IllnessId = String.Empty;
            BillAmount = 0;
            SettlementTotal = 0;
            TotalSharedAmount = 0;
            BillStatus = -1;
        }
    }

    public class SettlementTypeInfo
    {
        public int SettlementTypeCode;
        public String SettlementTypeValue;

        public SettlementTypeInfo()
        {
            SettlementTypeCode = 0;
            SettlementTypeValue = String.Empty;
        }

        public SettlementTypeInfo(int code, String value)
        {
            SettlementTypeCode = code;
            SettlementTypeValue = value;
        }
    }

    public class PersonalResponsiblityTypeInfo
    {
        public int PersonalResponsibilityTypeCode;
        public String PersonalResponsibilityTypeValue;

        public PersonalResponsiblityTypeInfo()
        {
            PersonalResponsibilityTypeCode = 0;
            PersonalResponsibilityTypeValue = String.Empty;
        }

        public PersonalResponsiblityTypeInfo(int code, String value)
        {
            PersonalResponsibilityTypeCode = code;
            PersonalResponsibilityTypeValue = value;
        }
    }


    public class StaffInfo
    {
        public int StaffId;
        public String StaffName;

        public StaffInfo()
        {
            StaffId = -1;
            StaffName = String.Empty;
        }
        public StaffInfo(int staff_id, String staff_name)
        {
            StaffId = staff_id;
            StaffName = staff_name;
        }
    }

    public class PaymentMethod
    {
        public int PaymentMethodId;
        public String PaymentMethodValue;

        public PaymentMethod()
        {
            PaymentMethodId = 0;
            PaymentMethodValue = String.Empty;
        }

        public PaymentMethod(int id, String value)
        {
            PaymentMethodId = id;
            PaymentMethodValue = value;
        }
    }

    public class CreditCardInfo
    {
        public int CreditCardId;
        public String CreditCardNo;

        public CreditCardInfo()
        {
            CreditCardId = 0;
            CreditCardNo = String.Empty;
        }

        public CreditCardInfo(int id, String card_no)
        {
            CreditCardId = id;
            CreditCardNo = card_no;
        }
    }
    //public class MedicalFormReceived
    //{
    //    public int NPF_Form;
    //    public int IB_Form;
    //    public int POP_Form;
    //    public int MedicalRecord_Form;
    //    public int Unknown_Form;

    //    public MedicalFormReceived()
    //    {
    //        NPF_Form = 0;
    //        IB_Form = 0;
    //        POP_Form = 0;
    //        MedicalRecord_Form = 0;
    //        Unknown_Form = 0;
    //    }

    //    public MedicalFormReceived(int npf_form, int ib_form, int pop_form, int medical_form, int unknown_form)
    //    {
    //        NPF_Form = npf_form;
    //        IB_Form = ib_form;
    //        POP_Form = pop_form;
    //        MedicalRecord_Form = medical_form;
    //        Unknown_Form = unknown_form;
    //    }
    //}

    public class CaseInfo
    {
        public String CaseName;
        public String IndividualId;

        public CaseInfo()
        {
            CaseName = String.Empty;
            IndividualId = String.Empty;
        }

        public CaseInfo(String casename, String individual_id)
        {
            CaseName = casename;
            IndividualId = individual_id;
        }
    }

    public class CasedInfoDetailed
    {
        public String CaseId;
        public String ContactId;
        public DateTime CreateDate;
        public DateTime ModificationDate;
        public int CreateStaff;
        public int ModifyingStaff;
        public Boolean Status;
        public int NPF_Form;
        public int IB_Form;
        public int POP_Form;
        public int MedicalRecord_Form;
        public int Unknown_Form;
        //public Boolean NPF_Form;
        public String NPF_Form_File_Name;
        public String NPF_Form_Destination_File_Name;
        public DateTime? NPF_ReceivedDate;
        //public Boolean IB_Form;
        public String IB_Form_File_Name;
        public String IB_Form_Destination_File_Name;
        public DateTime? IB_ReceivedDate;
        //public Boolean POP_Form;
        public String POP_Form_File_Name;
        public String POP_Form_Destionation_File_Name;
        public DateTime? POP_ReceivedDate;
        //public Boolean MedRec_Form;
        public String MedRec_Form_File_Name;
        public String MedRec_Form_Destination_File_Name;
        public DateTime? MedRec_ReceivedDate;
        //public Boolean Unknown_Form;
        public String Unknown_Form_File_Name;
        public String Unknown_Form_Destination_File_Name;
        public DateTime? Unknown_ReceivedDate;
        public String Note;
        public String Log_Id;
        public Boolean AddBill_Form;
        public DateTime? AddBill_Received_Date;
        public String Remove_Log;
        public String Individual_Id;

        public CasedInfoDetailed()
        {
            CaseId = String.Empty;
            ContactId = String.Empty;
            CreateDate = DateTime.Today;
            ModificationDate = DateTime.Today;
            CreateStaff = 0;
            ModifyingStaff = 0;
            Status = false;
            NPF_Form = 0;
            NPF_Form_File_Name = String.Empty;
            NPF_Form_Destination_File_Name = String.Empty;
            NPF_ReceivedDate = DateTime.Today;
            NPF_ReceivedDate = null;
            IB_Form_File_Name = String.Empty;
            IB_Form_Destination_File_Name = String.Empty;
            IB_ReceivedDate = null;
            //IB_ReceivedDate = DateTime.Today;
            POP_Form = 0;
            POP_Form_File_Name = String.Empty;
            POP_Form_Destionation_File_Name = String.Empty;
            POP_ReceivedDate = null;
            //POP_ReceivedDate = DateTime.Today;
            MedicalRecord_Form = 0;
            MedRec_Form_File_Name = String.Empty;
            MedRec_Form_Destination_File_Name = String.Empty;
            MedRec_ReceivedDate = null;
            //MedRec_ReceivedDate = DateTime.Today;
            Unknown_Form = 0;
            Unknown_Form_File_Name = String.Empty;
            Unknown_Form_Destination_File_Name = String.Empty;
            Unknown_ReceivedDate = null;
            //Unknown_ReceivedDate = DateTime.Today;
            Note = String.Empty;
            Log_Id = String.Empty;
            AddBill_Form = false;
            AddBill_Received_Date = null;
            //AddBill_Received_Date = DateTime.Today;
            Remove_Log = String.Empty;
            Individual_Id = String.Empty;
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    // DateTimePickerColumn for DataGridView

    public class CalendarColumn : DataGridViewColumn
    {
        public CalendarColumn() : base(new CalendarCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                // Ensure that the cell used for the template is a CalendarCell.
                if (value != null &&
                    !value.GetType().IsAssignableFrom(typeof(CalendarCell)))
                {
                    throw new InvalidCastException("Must be a CalendarCell");
                }
                base.CellTemplate = value;
            }
        }
    }

    public class CalendarCell : DataGridViewTextBoxCell
    {

        public CalendarCell()
            : base()
        {
            // Use the short date format.
            this.Style.Format = "d";
        }

        public override void InitializeEditingControl(int rowIndex, object
            initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue,
                dataGridViewCellStyle);
            CalendarEditingControl ctl =
                DataGridView.EditingControl as CalendarEditingControl;
            // Use the default row value when Value property is null.
            if (this.Value == null)
            {
                ctl.Value = (DateTime)this.DefaultNewRowValue;
            }
            //else  : original code
            else if (this.Value.ToString() != String.Empty)
            {
                ctl.Value = (DateTime)this.Value;
            }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing control that CalendarCell uses.
                return typeof(CalendarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.

                return typeof(DateTime);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return DateTime.Now;
            }
        }
    }

    class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public CalendarEditingControl()
        {
            this.Format = DateTimePickerFormat.Short;
        }

        // Implements the IDataGridViewEditingControl.EditingControlFormattedValue 
        // property.
        public object EditingControlFormattedValue
        {
            get
            {
                return this.Value.ToShortDateString();
            }
            set
            {
                if (value is String)
                {
                    try
                    {
                        // This will throw an exception of the string is 
                        // null, empty, or not in the format of a date.
                        this.Value = DateTime.Parse((String)value);
                    }
                    catch
                    {
                        // In the case of an exception, just use the 
                        // default value so we're not left with a null
                        // value.
                        this.Value = DateTime.Now;
                    }
                }
            }
        }

        // Implements the 
        // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(
            DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the 
        // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex 
        // property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }
            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey 
        // method.
        public bool EditingControlWantsInputKey(
            Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit 
        // method.
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl
        // .RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }
            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell
            // have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(eventargs);
        }
    }

}


