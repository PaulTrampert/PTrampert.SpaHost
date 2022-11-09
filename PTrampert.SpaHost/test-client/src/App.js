import logo from './logo.svg';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import AppNav from "./AppNav";
import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';

function App() {
  return (
    <div className="App">
        <Container>
            <Row>
                <AppNav />
            </Row>
        </Container>
    </div>
  );
}

export default App;
