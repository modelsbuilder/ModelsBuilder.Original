<%@ Control Language="C#" AutoEventWireup="true" %>

<script runat="server">
    protected override void OnLoad(EventArgs e)
    {
        // <%@ Register TagPrefix="umb" Namespace="ClientDependency.Core.Controls" Assembly="ClientDependency.Core" %>
        // <umb:CssInclude runat="server" FilePath="propertypane/style.css" PathNameAlias="UmbracoClient" />

        var ver = Umbraco.Core.Configuration.UmbracoVersion.Current;
        if (ver.Major >= 7) return;
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
				<h2>Generate Models</h2>
				<img class="dashboardIcon" alt="Umbraco" src="/UserControls/Zbu/ModelsBuilder/logo32x32.png">
				<div id="generateModelsPane" style="min-height: 240px;">
					<p>Click to generate models. 
                        <%=ConfigurationManager.AppSettings["Zbu.ModelsBuilder.AspNet.BuildModels"] == "true" 
                            ? "Beware! It will restart the application." : "" %>
                        </p>
					<p>
						<button id="generateModels">Generate</button>
					</p>
				</div>
				<div style="display:none;min-height: 240px;" id="generateModelsRun">
					<span id="generateModelsRunMessage">Please wait...</span>
					<br />&nbsp;<br />
					<span id="generateModelsRunProgress" style="color:#999999;"/>
				</div>
			</div>
		</div>
	</div>
</div>
