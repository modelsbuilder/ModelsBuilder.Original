<%@ Control Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="Umbraco.ModelsBuilder.AspNet.Dashboard" %>

<script runat="server">
    protected override void OnLoad(EventArgs e)
    {
        phGenerate.Visible = DashboardHelper.CanGenerate();
        phGenerateWarning.Visible = DashboardHelper.GenerateRestarts();
        txtReport.Text = DashboardHelper.Report();
        txtGenerate.Text = DashboardHelper.GenerateLabel();

        if (DashboardHelper.IsUmbraco6())
            Page.Controls.Add(DashboardHelper.Umbraco6Control());
    }
</script>

<script type="text/javascript">
    jQuery(document).ready(function () {

        function buildModelsOnServer(callback) {
            // encodeURIComponent(args)...
            $.getJSON('<%=DashboardHelper.BuildUrl()%>', function (json) {
		        callback(json);
		    });
		}

		function nl2br(s) {
		    s = s.replace(/\r/g, '');
		    s = s.replace(/\n/g, '<br />');
		    return s;
		}

		jQuery('#generateModels').click(function (event) {
		    jQuery('#generateModelsPane').hide();
		    jQuery('#generateModelsRun').show();

		    buildModelsOnServer(function (json) {
		        if (json.Success) {
		            $('#generateModelsRunMessage').html('Success! Reloading...');
		            window.location.reload();
		        }
		        else {
		            $('#generateModelsRunMessage').html('Failed. I\'m so sorry.');
		            $('#generateModelsRunProgress').html(nl2br(json.Message));
		        }
		    });

		    event.preventDefault();
		    return false;
		});
	});
</script>

<div class="propertypane">
	<div>
		<div class="propertyItem">
			<div class="dashboardWrapper">
				<h2>Umbraco.ModelsBuilder</h2>
                <div style="margin-top: 24px;">
                    <asp:Literal runat="server" ID="txtReport" />
                </div>
                <asp:PlaceHolder runat="server" ID="phGenerate">
                    <div style="margin-top: 24px;">
				        <div id="generateModelsPane" style="min-height: 240px;">
				            <p><asp:Literal runat="server" ID="txtGenerate"/></p>
                            <asp:PlaceHolder runat="server" ID="phGenerateWarning">
                                <p style="color:red;">Beware! This will restart the application.</p>
                            </asp:PlaceHolder>
						    <p><button id="generateModels">Generate</button></p>
				        </div>
				        <div style="display:none;min-height: 240px;" id="generateModelsRun">
					        <span id="generateModelsRunMessage">Please wait...</span>
					        <br />&nbsp;<br />
					        <span id="generateModelsRunProgress" style="color:#999999;"/>
				        </div>
                    </div>
                </asp:PlaceHolder>
			</div>
		</div>
	</div>
</div>
