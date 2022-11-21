import Nav from 'react-bootstrap/Nav';
import Navbar from 'react-bootstrap/Navbar';
import Container from "react-bootstrap/Container";
import {Button} from "react-bootstrap";
import Cookies from 'js-cookie';

const xsrfElemName = "antiforgeryToken";

async function onLogout(e) {
    e.preventDefault();
    let form = e.target;
    let xsrfResponse = await fetch('/antiforgery', {credentials:"include"});
    if (!xsrfResponse.ok)
    {
        alert("Failed to get antiforgery token");
    }

    form.elements[xsrfElemName].value= Cookies.get('XSRF-TOKEN');
    form.submit();
}

function AppNav() {

    return (
        <Navbar expand={true}>
            <Container>
                <Navbar.Brand>PTrampert.SpaHost</Navbar.Brand>
                <Nav className="justify-content-end">
                    <form action="/logout" method="POST" onSubmit={onLogout}>
                        <input type="hidden" name={xsrfElemName} />
                        <Nav.Link as={Button} variant="link" type="submit">Logout</Nav.Link>
                    </form>
                </Nav>
            </Container>
        </Navbar>
    );
}

export default AppNav;