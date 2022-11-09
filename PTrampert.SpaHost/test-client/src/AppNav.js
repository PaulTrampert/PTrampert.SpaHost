import Nav from 'react-bootstrap/Nav';
import Navbar from 'react-bootstrap/Navbar';
import Container from "react-bootstrap/Container";
import {Component} from "react";
import {Button} from "react-bootstrap";

class AppNav extends Component {


    render() {
        return (
            <Navbar expand={true}>
                <Container>
                    <Navbar.Brand>PTrampert.SpaHost</Navbar.Brand>
                    <Nav className="justify-content-end">
                        <form action="/logout" method="POST">
                            <Nav.Link as={Button} variant="link" type="submit">Logout</Nav.Link>
                        </form>
                    </Nav>
                </Container>
            </Navbar>
        );
    }
}

export default AppNav;