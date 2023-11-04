namespace GroupChat
{
	public partial class IDD_DELETEGROUP : Form
	{
		public IDD_DELETEGROUP()
		{
			InitializeComponent();
			GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, true);
		}

		private void IDOK_Click(object sender, EventArgs e)
		{
			GroupChat.Main.SetStatus("Group Deleted");
			Close();
		}

		private void IDC_CB_IDENTITY_SelectedIndexChanged(object sender, EventArgs e)
		{
			GroupChat.RefreshGroupCombo(IDC_CB_GROUP, GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY));
		}
	}
}
