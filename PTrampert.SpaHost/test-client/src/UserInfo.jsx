import {Component} from "react";
import Container from "react-bootstrap/Container";

export default class UserInfo extends Component{
    constructor(props) {
        super(props);
        this.state = {
            userInfo: []
        }
    }

    componentDidMount = async () => {
        let response = await fetch('/UserInfo', {credentials: "include"});
        if (response.ok) {
            let userInfo = await response.json();
            this.setState({userInfo})
        }
    }

    render() {
        let {userInfo} = this.state;
        return (
            <Container>
                <dl>
                {userInfo.map(c => (
                    <>
                        <dt>{c.type}</dt>
                        <dd>{c.value}</dd>
                    </>
                ))}
                </dl>
            </Container>
        );
    }
}