<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="GenerateModels.ascx.cs" Inherits="Zbu.ModelsBuilder.AspNet.GenerateModelsDashboard" %>
<%@ Register TagPrefix="umb" Namespace="ClientDependency.Core.Controls" Assembly="ClientDependency.Core" %>

<umb:CssInclude runat="server" FilePath="propertypane/style.css" PathNameAlias="UmbracoClient" />

<script type="text/javascript">
    jQuery(document).ready(function () {
        jQuery('#generateModels').click(function (event) {
            jQuery('#generateModelsPane').hide();
            jQuery('#generateModelsWait').show();

            $.get('/umbraco/dashboard.aspx?app=developer&generate=GenerateModelsDashBoardPleaseGenerate');

            var restartProgress = '████████████████████';
            var restartCountDown = 12 * 2 + 1; // half-second

            function tick() {
                restartCountDown -= 1;
                if (restartCountDown == 0) {
                    $('#restartMessage').html('Reloading...');
                    //window.top.location.reload();
                    window.location.reload();
                }
                else {
                    $('#restartMessage').html(restartProgress.substring(0, restartCountDown));
                    setTimeout(tick, 500);
                }
            }

            tick();

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
				<img class="dashboardIcon" alt="Umbraco" src="./dashboard/images/logo32x32.png">
				<div id="generateModelsPane" style="min-height: 240px;">
					<p>Click to generate models, will restart the app.</p>
					<p>
						<button id="generateModels">Generate</button>
					</p>
				</div>
				<div style="display:none;min-height: 240px;" id="generateModelsWait">
					Please wait...<br />&nbsp;<br />
					<span id="restartMessage" style="color:#999999;"/>
				</div>
			</div>
		</div>
	</div>
</div>
