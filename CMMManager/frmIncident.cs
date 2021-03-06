﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Globalization;

namespace CMMManager
{
    public partial class frmIncident : Form
    {
        public IncidentOption SelectedOption;

        public String IndividualId = String.Empty;
        public String CaseId = String.Empty;
        public String IllnessId = String.Empty;
        public String ICD10Code = String.Empty;
        public int nLoggedInId;

        //public Dictionary<int, String> dicProgram;

        public SqlConnection connRNDB;
        public String strRNDBConnString = String.Empty;

        public SelectedIncident IncidentSelected;
        public Boolean bIncidentSelected = true;
        public Boolean bIncidentCanceled = false;

        //private String strSqlQueryForIncident = String.Empty;
        //private String strSqlInsertNewIncident = String.Empty;

        private Dictionary<String, int> dicProgramId;

        private delegate void AddRowToIncidents(DataGridViewRow row);
        private delegate void RemoveRowIncidents(int nRow);
        private delegate void RemoveAllRowIncidents();


        public frmIncident()
        {
            InitializeComponent();

            strRNDBConnString = @"Data Source=CMM-2014U\CMM; Initial Catalog=RN_DB; Integrated Security=True";
            connRNDB = new SqlConnection(strRNDBConnString);

            IncidentSelected = new SelectedIncident();

            //dicProgram = new Dictionary<int, string>();
            dicProgramId = new Dictionary<string, int>();

        }

        private void frmIncident_Load(object sender, EventArgs e)
        {
            String strSqlQueryForIncident = "select [dbo].[tbl_incident].[incident_id], [dbo].[tbl_incident].[individual_id], [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_id], " +
                                            "[dbo].[tbl_incident].[CreateDate], [dbo].[tbl_program].[ProgramName], [dbo].[tbl_incident].[IncidentNote] " +
                                            "from ([dbo].[tbl_incident] " +
                                            "inner join [dbo].[tbl_illness] on [dbo].[tbl_incident].[Illness_id] = [dbo].[tbl_illness].[Illness_Id]) " +
                                            "inner join [dbo].[tbl_program] on [dbo].[tbl_incident].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                                            "where [dbo].[tbl_incident].[individual_id] = @IndividualId and " +
                                            "[dbo].[tbl_incident].[Case_id] = @CaseId and " +
                                            "[dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code and " +
                                            "[dbo].[tbl_incident].[IsDeleted] = 0 " +
                                            "order by [dbo].[tbl_incident].[incident_id]";

            SqlCommand cmdQueryForIncident = new SqlCommand(strSqlQueryForIncident, connRNDB);
            cmdQueryForIncident.CommandType = CommandType.Text;
            cmdQueryForIncident.CommandText = strSqlQueryForIncident;

            cmdQueryForIncident.Parameters.AddWithValue("@IndividualId", IndividualId);
            cmdQueryForIncident.Parameters.AddWithValue("@CaseId", CaseId);
            cmdQueryForIncident.Parameters.AddWithValue("@ICD10Code", ICD10Code);

            SqlDependency dependencyIncident = new SqlDependency(cmdQueryForIncident);
            dependencyIncident.OnChange += new OnChangeEventHandler(OnIncidentListChange);

            if (connRNDB.State == ConnectionState.Open)
            {
                connRNDB.Close();
                connRNDB.Open();
            }
            else if (connRNDB.State == ConnectionState.Closed) connRNDB.Open();

            SqlDataReader rdrIncidents = cmdQueryForIncident.ExecuteReader();

            if (rdrIncidents.HasRows)
            {
                gvIncidents.Rows.Clear();

                while (rdrIncidents.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(0) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetDateTime(4).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(5) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(6) });

                    gvIncidents.Rows.Add(row);
                }
            }

            if (connRNDB.State == ConnectionState.Open) connRNDB.Close();

            if (IncidentSelected.IncidentId != String.Empty)
            {
                for (int i = 0; i < gvIncidents.RowCount; i++)
                {
                    if (IncidentSelected.IncidentId == gvIncidents[1, i].Value.ToString()) gvIncidents[0, i].Value = true;
                }
            }

            String strSqlQueryForProgramId = "select [dbo].[tbl_program].[ProgramName], [dbo].[tbl_program].[Program_Id] from [dbo].[tbl_program]";

            SqlCommand cmdQueryForProgramId = new SqlCommand(strSqlQueryForProgramId, connRNDB);
            cmdQueryForProgramId.CommandType = CommandType.Text;

            if (connRNDB.State == ConnectionState.Open)
            {
                connRNDB.Close();
                connRNDB.Open();
            }
            else if (connRNDB.State == ConnectionState.Closed) connRNDB.Open();
            SqlDataReader rdrProgramId = cmdQueryForProgramId.ExecuteReader();
            if (rdrProgramId.HasRows)
            {
                while(rdrProgramId.Read())
                {
                    if (!rdrProgramId.IsDBNull(0) && !rdrProgramId.IsDBNull(1)) dicProgramId.Add(rdrProgramId.GetString(0).Trim(), rdrProgramId.GetInt16(1));
                }
            }
            if (connRNDB.State == ConnectionState.Open) connRNDB.Close();

        }

        private void AddRowToIncidentsSafely(DataGridViewRow row)
        {
            gvIncidents.BeginInvoke(new AddRowToIncidents(AddNewRowToIncidents), row);
        }

        private void ClearIncidentsSafely()
        {
            gvIncidents.BeginInvoke(new RemoveAllRowIncidents(RemoveAllRowsIncidents));
        }

        private void AddNewRowToIncidents(DataGridViewRow row)
        {
            gvIncidents.Rows.Add(row);
        }

        private void RemoveRowFromIncidents(int i)
        {
            gvIncidents.Rows.RemoveAt(i);
        }

        private void RemoveAllRowsIncidents()
        {
            gvIncidents.Rows.Clear();
        }

        private void OnIncidentListChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                SqlDependency dependency = sender as SqlDependency;
                dependency.OnChange -= OnIncidentListChange;

                UpdateGridViewIncidentList();
            }
        }

        private void UpdateGridViewIncidentList()
        {
            //String strSqlQueryForIncident = "select [dbo].[tbl_incident].[incident_id], [dbo].[tbl_incident].[individual_id], [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_id], " +
            //                                "[dbo].[tbl_incident].[CreateDate], [dbo].[tbl_incident].[Program_id], [dbo].[tbl_incident].[IncidentNote] " +
            //                                "from [dbo].[tbl_incident] " +
            //                                "where [dbo].[tbl_incident].[individual_id] = @IndividualId and " +
            //                                "[dbo].[tbl_incident].[Case_id] = @CaseId and " +
            //                                "[dbo].[tbl_incident].[Illness_id] = @IllnessId";

            String strSqlQueryForIncident = "select [dbo].[tbl_incident].[incident_id], [dbo].[tbl_incident].[individual_id], [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_id], " +
                                            "[dbo].[tbl_incident].[CreateDate], [dbo].[tbl_program].[ProgramName], [dbo].[tbl_incident].[IncidentNote] " +
                                            "from ([dbo].[tbl_incident] inner join [dbo].[tbl_illness] on [dbo].[tbl_incident].[Illness_id] = [dbo].[tbl_illness].[Illness_Id]) " +
                                            "inner join [dbo].[tbl_program] on [dbo].[tbl_incident].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                                            "where [dbo].[tbl_incident].[individual_id] = @IndividualId and " +
                                            "[dbo].[tbl_incident].[Case_id] = @CaseId and " +
                                            "[dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code and " +
                                            "[dbo].[tbl_incident].[IsDeleted] = 0 " +
                                            "order by [dbo].[tbl_incident].[incident_id]";

            SqlCommand cmdQueryForIncident = new SqlCommand(strSqlQueryForIncident, connRNDB);
            cmdQueryForIncident.CommandType = CommandType.Text;
            cmdQueryForIncident.CommandText = strSqlQueryForIncident;
            cmdQueryForIncident.Parameters.AddWithValue("@IndividualId", IndividualId);
            cmdQueryForIncident.Parameters.AddWithValue("@CaseId", CaseId);
            cmdQueryForIncident.Parameters.AddWithValue("@ICD10Code", ICD10Code);

            SqlDependency dependencyIncident = new SqlDependency(cmdQueryForIncident);
            dependencyIncident.OnChange += new OnChangeEventHandler(OnIncidentListChange);

            if (connRNDB.State == ConnectionState.Open)
            {
                connRNDB.Close();
                connRNDB.Open();
            }
            else if (connRNDB.State == ConnectionState.Closed) connRNDB.Open();
            SqlDataReader rdrIncidents = cmdQueryForIncident.ExecuteReader();

            if (IsHandleCreated) ClearIncidentsSafely();
            else gvIncidents.Rows.Clear();
            //gvIncidents.Rows.Clear();
            if (rdrIncidents.HasRows)
            {
                while (rdrIncidents.Read())
                {
                    DataGridViewRow row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(0) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(3) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetDateTime(4).ToString("MM/dd/yyyy") });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(5) });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(6) });

                    if (IsHandleCreated) AddRowToIncidentsSafely(row);
                    else gvIncidents.Rows.Add(row);
                }
            }

            if (connRNDB.State == ConnectionState.Open) connRNDB.Close();
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            frmIncidentCreationPage frmIncidentCreation = new frmIncidentCreationPage();

            frmIncidentCreation.strIndividualId = IndividualId;
            frmIncidentCreation.strCaseId = CaseId;
            frmIncidentCreation.strIllnessId = IllnessId;
            frmIncidentCreation.nLoggedInId = nLoggedInId;

            frmIncidentCreation.mode = frmIncidentCreationPage.IncidentMode.AddNew;

            if (frmIncidentCreation.ShowDialog(this) == DialogResult.OK)
            {
                // Do when an incident is created successfully
                //String strSqlQueryForIncident = "select [dbo].[tbl_incident].[incident_id], [dbo].[tbl_incident].[individual_id], [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_id], " +
                //                                "[dbo].[tbl_incident].[CreateDate], [dbo].[tbl_incident].[Program_id], [dbo].[tbl_incident].[IncidentNote] " +
                //                                "from ([dbo].[tbl_incident] inner join [dbo].[tbl_illness] on [dbo].[tbl_incident].[Illness_id] = [dbo].[tbl_illness].[Illness_Id]) " +
                //                                "where [dbo].[tbl_incident].[individual_id] = @IndividualId and " +
                //                                "[dbo].[tbl_incident].[Case_id] = @CaseId and " +
                //                                "[dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code";
                String strSqlQueryForIncident = "select [dbo].[tbl_incident].[incident_id], [dbo].[tbl_incident].[individual_id], [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_id], " +
                                                "[dbo].[tbl_incident].[CreateDate], [dbo].[tbl_program].[ProgramName], [dbo].[tbl_incident].[IncidentNote] " +
                                                "from ([dbo].[tbl_incident] inner join [dbo].[tbl_illness] on [dbo].[tbl_incident].[Illness_id] = [dbo].[tbl_illness].[Illness_Id]) " +
                                                "inner join [dbo].[tbl_program] on [dbo].[tbl_incident].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                                                "where [dbo].[tbl_incident].[individual_id] = @IndividualId and " +
                                                "[dbo].[tbl_incident].[Case_id] = @CaseId and " +
                                                "[dbo].[tbl_illness].[ICD_10_Id] = @ICD10Code and " +
                                                "[dbo].[tbl_incident].[IsDeleted] = 0 " +
                                                "order by [dbo].[tbl_incident].[incident_id]";

                SqlCommand cmdQueryForIncident = new SqlCommand(strSqlQueryForIncident, connRNDB);
                cmdQueryForIncident.CommandType = CommandType.Text;
                cmdQueryForIncident.CommandText = strSqlQueryForIncident;

                cmdQueryForIncident.Parameters.AddWithValue("@IndividualId", IndividualId);
                cmdQueryForIncident.Parameters.AddWithValue("@CaseId", CaseId);
                cmdQueryForIncident.Parameters.AddWithValue("@ICD10Code", ICD10Code);

                //SqlDependency dependencyIncident = new SqlDependency(cmdQueryForIncident);
                //dependencyIncident.OnChange += new OnChangeEventHandler(OnIncidentListChange);

                if (connRNDB.State == ConnectionState.Open)
                {
                    connRNDB.Close();
                    connRNDB.Open();
                }
                else if (connRNDB.State == ConnectionState.Closed) connRNDB.Open();

                SqlDataReader rdrIncidents = cmdQueryForIncident.ExecuteReader();

                gvIncidents.Rows.Clear();
                if (rdrIncidents.HasRows)
                {
                    while (rdrIncidents.Read())
                    {
                        DataGridViewRow row = new DataGridViewRow();

                        row.Cells.Add(new DataGridViewCheckBoxCell { Value = false });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(0) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetInt32(3) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetDateTime(4) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(5) });
                        row.Cells.Add(new DataGridViewTextBoxCell { Value = rdrIncidents.GetString(6) });

                        gvIncidents.Rows.Add(row);
                    }
                }

                if (connRNDB.State == ConnectionState.Open) connRNDB.Close();

            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            //DialogResult = DialogResult.None;
            //bIncidentSelected = false;
            SelectedOption = IncidentOption.Close;
            Close();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gvIncidents.RowCount; i++)
            {
                if ((bool)gvIncidents[0, i].Value == true)
                {
                    IncidentSelected.IncidentId = gvIncidents[1, i].Value.ToString();
                    IncidentSelected.IllnessId = gvIncidents[2, i].Value.ToString();
                    //IncidentSelected.ProgramId = int.Parse(gvIncidents[4, i].Value.ToString());

                    String strProgramName = gvIncidents[4, i].Value.ToString().Trim();
                    //IncidentSelected.ProgramId = dicProgramId[strProgramName];
                    IncidentSelected.ProgramId = dicProgramId[gvIncidents[4, i].Value.ToString().Trim()];
                    IncidentSelected.Note = gvIncidents[5, i].Value.ToString();

                    bIncidentSelected = true;
                    DialogResult = DialogResult.OK;
                    SelectedOption = IncidentOption.Select;

                    return;
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            int nIncidentId = 0;
            int nIllnessId = 0;
            int result = 0;

            for (int i = 0; i < gvIncidents.Rows.Count; i++)
            {
                //nIncidentId = int.Parse(gvIncidents["Incident_Id", i]?.Value?.ToString())
                if (int.TryParse(gvIncidents["Incident_Id", i]?.Value?.ToString(), NumberStyles.Number, new CultureInfo("en-US"), out result)) nIncidentId = result;
                if (int.TryParse(gvIncidents["Illness_Id", i]?.Value?.ToString(), NumberStyles.Number, new CultureInfo("en-US"), out result)) nIllnessId = result;
            }

            if (nIncidentId != 0 && nIllnessId != 0)
            {
                frmIncidentCreationPage frm = new frmIncidentCreationPage();

                frm.strIncidentId = nIncidentId.ToString();
                frm.strCaseId = CaseId;
                frm.strIndividualId = IndividualId;
                frm.nLoggedInId = nLoggedInId;
                frm.mode = frmIncidentCreationPage.IncidentMode.Edit;

                //frm.ShowDialog();
             

                DialogResult dlgEditIncident = frm.ShowDialog();

                //if (dlgEditIncident == DialogResult.OK)
                //{
                //    Close();
                //}
                //if (dlgEditIncident == DialogResult.OK)
                //{

                //}
                
                //String strSqlQueryForIncident = "select [dbo].[tbl_incident].[Case_id], [dbo].[tbl_incident].[Illness_Id], [dbo].[tbl_program].[ProgramName], [dbo].[tbl_program].[CreateDate], " +
                //                                "[dbo].[tbl_program].[ModifiDate], [dbo].[tbl_program].[IncidentNote] from [dbo].[tbl_program] " +
                //                                "inner join [dbo].[tbl_program] on [dbo].[tbl__incident].[Program_id] = [dbo].[tbl_program].[Program_Id] " +
                //                                "where [dbo].[tbl_incident].[Incident_id] = @IncidentId and [dbo].[tbl_incident].[Individual_id] = @IndividualId";

                //SqlCommand cmdQueryForIncident = new SqlCommand(strSqlQueryForIncident, connRNDB);
                //cmdQueryForIncident.CommandType = CommandType.Text;

                //cmdQueryForIncident.Parameters.AddWithValue("@IncidentId", nIncidentId);
                //cmdQueryForIncident.Parameters.AddWithValue("@IndividualId", IndividualId);

                //connRNDB.Open();
                //SqlDataReader rdrIncident = cmdQueryForIncident.ExecuteReader();
                //if (rdrIncident.HasRows)
                //{
                //    rdrIncident.Read();
                //    //if (!rdrIncident.IsDBNull(0))
                //}
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (gvIncidents.Rows.Count > 0)
            {
                //int nTotalIncidentSelected = 0;
                List<int> lstIncidentsToDelete = new List<int>();

                for(int i = 0; i < gvIncidents.Rows.Count; i++)
                {
                    if ((Boolean)gvIncidents["Selected", i].Value == true)
                    {
                        //nTotalIncidentSelected++;
                        lstIncidentsToDelete.Add(Int32.Parse(gvIncidents["Incident_Id", i]?.Value?.ToString()));
                    }
                }
                //if (nTotalIncidentSelected > 0)

                if (lstIncidentsToDelete.Count > 0)
                {
                    try
                    {
                        DialogResult dlgResultConfirm = MessageBox.Show("Are you sure to delete these incidents?", "Waring", MessageBoxButtons.YesNo);

                        if (dlgResultConfirm == DialogResult.Yes)
                        {
                            Boolean bErrorFlag = false;

                            if (connRNDB.State == ConnectionState.Open)
                            {
                                connRNDB.Close();
                                connRNDB.Open();
                            }
                            else if (connRNDB.State == ConnectionState.Closed) connRNDB.Open();

                            // Begin transaction here
                            SqlTransaction transDelete = connRNDB.BeginTransaction();

                            for (int i = 0; i < lstIncidentsToDelete.Count; i++)
                            {
                                String strSqlDeleteIncident = "update [dbo].[tbl_incident] set [dbo].[tbl_incident].[IsDeleted] = 1 where [dbo].[tbl_incident].[Incident_id] = @IncidentNo";

                                SqlCommand cmdDeleteIncident = new SqlCommand(strSqlDeleteIncident, connRNDB, transDelete);;
                                cmdDeleteIncident.CommandType = CommandType.Text;

                                cmdDeleteIncident.Parameters.AddWithValue("@IncidentNo", lstIncidentsToDelete[i]);

                                int nRowDeleted = cmdDeleteIncident.ExecuteNonQuery();
                                //if (nRowDeleted == 0) bErrorFlag = true;
                            }

                            transDelete.Commit();

                            //if (bErrorFlag)
                            //{
                            //    MessageBox.Show("Some of incident have not been deleted.", "Error");
                            //    return;
                            //}
                        }
                        if (dlgResultConfirm == DialogResult.No)
                        {
                            return;
                        }
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                    finally
                    {
                        if (connRNDB.State == ConnectionState.Open) connRNDB.Close();
                    }
                }
                else if (lstIncidentsToDelete.Count == 0)
                {
                    MessageBox.Show("Please select incidents to delete");
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SelectedOption = IncidentOption.Cancel;
            Close();
        }

        private void gvIncidents_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView gv = sender as DataGridView;

            int nClicked = e.RowIndex;

            if ((Boolean)gv[0, e.RowIndex].Value == true) gv[0, e.RowIndex].Value = false;
            else if ((Boolean)gv[0, e.RowIndex].Value == false) gv[0, e.RowIndex].Value = true;

            for (int i = 0; i < gv.RowCount; i++)
            {
                if (i != e.RowIndex) gv[0, i].Value = false;
            }
        }
    }

    public class SelectedIncident
    {
        private String strIncidentId;
        private String strCaseId;
        private String strIllnessId;
        private int nIncidentStatus;
        private int nProgramId;
        private String strNote;

        public String IncidentId
        {
            get { return strIncidentId; }
            set { strIncidentId = value; }
        }

        public String CaseId
        {
            get { return strCaseId; }
            set { strCaseId = value; }
        }

        public String IllnessId
        {
            get { return strIllnessId; }
            set { strIllnessId = value; }
        }

        public int IncidentStatus
        {
            get { return nIncidentStatus; }
            set { nIncidentStatus = value; }
        }

        public int ProgramId
        {
            get { return nProgramId; }
            set { nProgramId = value; }
        }

        public String Note
        {
            get { return strNote; }
            set { strNote = value; }
        }

        public SelectedIncident()
        {
            strIncidentId = String.Empty;
            strCaseId = String.Empty;
            strIllnessId = String.Empty;
            nIncidentStatus = 2;
            nProgramId = 0;
            Note = String.Empty;
        }

        public SelectedIncident(String incident_id, String case_id, String illness_id, int incident_status, int program_id, String note)
        {
            strIncidentId = incident_id;
            strCaseId = case_id;
            strIllnessId = illness_id;
            nIncidentStatus = incident_status;
            nProgramId = program_id;
            Note = note;
        }
    }
}
