import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import AppNav from "./AppNav";
import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';
import UserInfo from "./UserInfo";

function App() {
  return (
    <div className="App">
        <Container>
            <Row>
                <AppNav />
                <UserInfo />
            </Row>
        </Container>
    </div>
  );
}

export default App;
