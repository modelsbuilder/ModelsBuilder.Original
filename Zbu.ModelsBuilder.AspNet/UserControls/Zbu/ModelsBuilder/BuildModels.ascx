<%@ Control Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="Zbu.ModelsBuilder.Configuration" %>

<script runat="server">
    protected override void OnLoad(EventArgs e)
    {
        // <%@ Register TagPrefix="umb" Namespace="ClientDependency.Core.Controls" Assembly="ClientDependency.Core" %>
        // <umb:CssInclude runat="server" FilePath="propertypane/style.css" PathNameAlias="UmbracoClient" />

        phGenerate.Visible = Config.EnableAppDataModels || Config.EnableAppCodeModels || Config.EnableDllModels;
        phGenerateWarning.Visible = Config.EnableAppCodeModels || Config.EnableDllModels;

        var sb = new StringBuilder();
        sb.Append("Config: ");
        if (Config.EnableApi) sb.Append(" +EnableApi");
        if (Config.EnableAppDataModels) sb.Append(" +EnableAppDataModels");
        if (Config.EnableAppCodeModels) sb.Append(" +EnableAppCodeModels");
        if (Config.EnableDllModels) sb.Append(" +EnableDllModels");
        if (Config.EnableLiveModels) sb.Append(" +EnableLiveModels");
        if (Config.EnablePublishedContentModelsFactory) sb.Append(" +EnablePublishedContentModelsFactory");
        sb.AppendFormat("<br />Config.ModelsNameSpace: \"{0}\"", Config.ModelsNamespace);
        txtReport.Text = sb.ToString();

        var ver = Umbraco.Core.Configuration.UmbracoVersion.Current;        
        if (ver.Major == 6)
            Umbraco6();
    }

    private void Umbraco6()
    {
        var css = new ClientDependency.Core.Controls.CssInclude();
        css.FilePath = "propertypane/style.css";
        css.PathNameAlias = "UmbracoClient";
        Page.Controls.Add(css);
    }
</script>

<script type="text/javascript">
    jQuery(document).ready(function () {

        function buildModelsOnServer(callback) {
            // encodeURIComponent(args)...
            $.getJSON('<%=Zbu.ModelsBuilder.AspNet.ModelsBuilderApiController.BuildModelsUrl%>', function (json) {
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
				<h2>Zbu.ModelsBuilder</h2>
				<img class="dashboardIcon" alt="Umbraco" src="/UserControls/Zbu/ModelsBuilder/logo32x32.png">
                <div style="margin-top: 24px;">
                    <asp:Literal runat="server" ID="txtReport" />
                </div>
                <asp:PlaceHolder runat="server" ID="phGenerate">
                    <div style="margin-top: 24px;">
				        <div id="generateModelsPane" style="min-height: 240px;">
					        <p>Click button to generate models.</p>
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
