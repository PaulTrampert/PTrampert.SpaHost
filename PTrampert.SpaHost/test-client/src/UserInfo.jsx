import Container from "react-bootstrap/Container";

export default function UserInfo({userInfo}) {
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