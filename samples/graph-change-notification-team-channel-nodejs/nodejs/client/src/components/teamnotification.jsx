// <copyright file="dashboard.jsx" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// </copyright>

import React, { Component } from "react";
import * as microsoftTeams from "@microsoft/teams-js";
import axios from "axios";
import moment from 'moment'
import "../style/style.css";
import "../style/card.css";

class TeamNotification extends Component {
    constructor(props) {
        super(props);

        this.state = {
            teamId: "",
            notifications: [],
            teamsContext: {},
            pageId: ""
        }
    }

    componentDidMount() {
        microsoftTeams.app.initialize().then(() => {
            microsoftTeams.app.getContext().then((context) => {
                this.setState({ teamId: context.team.groupId });
                var url = window.location.href;
                var pageid = url.match(/\d+$/)[0];
                this.setState({ pageId: pageid });
                this.initializeData(context.team.groupId);
            })
        });
    }

    initializeData = async (teamId) => {
        var response = await axios.post(`/api/notifications/team?teamId=${teamId}`);

        if (response.status === 200) {
            var responseData = response.data;

            if (responseData) {
                var elements = [];

                responseData.forEach(item => {
                    elements.push(<div>
                        <p><b>Team Name :</b> {item.displayName}</p>

                        {(() => {
                            if (item.changeType === 'updated') {
                                return (<div><p><b>Description  : </b> When new Team is Edited and Deleted you will get notification as updated  </p>
                                    <p><b>Status         : </b><span className="statusColor"> {item.changeType}</span></p>
                                </div>);
                            }
                           
                            if (item.changeType === 'deleted') {
                                return (<div><p><b>Description  : </b> Team has deleted</p>
                                    <p><b>Status         : </b><span className="deleteStatus"> {item.changeType}</span></p>
                                </div>);
                            }
                        })()}
                       
                        <p><b>Date         :</b> {moment(item.createdDate).format('LLL')} <b>
                            <span className="headcolor">{moment(item.createdDate).fromNow()}</span></b></p>
                        <hr></hr>
                    </div>);
                });
                if (elements.length > 0) {
                    this.setState({ notifications: elements.reverse() });
                }
            }
        }
    }

    welcomeMessage = () => {
        return (
            <div>
                <h3 className="headcolor">Team Notifications</h3>
                <h4>Welcome to Teams Notification Tab</h4>
                <p>This Tab has successfully configured, you will get notifications of Team Update in this team</p>
            </div>
        );
    }

    render() {
        return (
            <div className="tag-container">
                <div>
                    {this.welcomeMessage()}
                    <hr></hr>
                    {this.state.notifications}
                </div>
            </div>
        )
    }
}

export default TeamNotification;