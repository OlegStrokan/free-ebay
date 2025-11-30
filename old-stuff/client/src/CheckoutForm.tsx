import React, { useEffect, useRef, useState } from "react";
import {
  loadStripe,
  Stripe,
  StripeCardElement,
  StripeElements,
} from "@stripe/stripe-js";

const stripePromise = loadStripe(
  "pk_test_51Rfi51QrFNrrAuUP6DvC3KzyhAq5DH1hIkausjxgaKuBLtvs0mVKkEgAdOZXdg4Cg6C5cIpZ7EfNP0QlWKkRt1B600K8QYreWL"
);

export const CheckoutForm: React.FC = () => {
  const [clientSecret, setClientSecret] = useState("");
  const [cardMounted, setCardMounted] = useState(false);
  const cardElementRef = useRef<StripeCardElement | null>(null);
  const elementsRef = useRef<StripeElements | null>(null);

  useEffect(() => {
    fetch("/checkout/payment", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ amount: 5000 }),
    })
      .then((res) => res.json())
      .then((data) => setClientSecret(data.clientSecret));
  }, []);

  useEffect(() => {
    if (!clientSecret || cardMounted) return;

    async function setupCard() {
      const stripe = await stripePromise;
      if (!stripe) {
        console.error("Stripe failed to initialize.");
        return;
      }
      const elements = stripe.elements();
      if (!elements) {
        console.error("Stripe elements failed to initialize.");
        return;
      }
      elementsRef.current = elements;
      const card = elements.create("card");
      card.mount("#card-element");
      cardElementRef.current = card;
      setCardMounted(true);
    }

    setupCard();
  }, [clientSecret, cardMounted]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const stripe = await stripePromise;
    if (!stripe || !elementsRef.current || !cardElementRef.current) {
      console.error("Stripe or elements not initialized.");
      return;
    }

    const { error, paymentIntent } = await stripe.confirmCardPayment(
      clientSecret,
      {
        payment_method: {
          card: cardElementRef.current,
          billing_details: { name: "Customer Name" },
        },
      }
    );

    if (error) {
      console.error("Payment failed:", error.message);
    } else if (paymentIntent && paymentIntent.status === "succeeded") {
      console.log("Payment succeeded:", paymentIntent);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div id="card-element" />
      <button type="submit" disabled={!clientSecret}>
        Pay
      </button>
    </form>
  );
};
